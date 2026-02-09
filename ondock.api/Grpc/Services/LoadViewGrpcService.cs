using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Ondock.Api.Grpc.V1;
using ondock.api.Data.Entities;
using ondock.api.DTOs.LoadView;
using ondock.api.Services.Interfaces;

namespace ondock.api.Grpc.Services;

[Authorize]
public class LoadViewGrpcService : LoadViewService.LoadViewServiceBase
{
    private readonly ILoadViewService _loadViewService;
    private readonly ILogger<LoadViewGrpcService> _logger;

    // Active streams for nearby requests (responders)
    private static readonly Dictionary<Guid, NearbyRequestStreamContext> ActiveNearbyStreams = new();
    private static readonly object NearbyStreamsLock = new();

    // Active streams for responses (requesters watching their request)
    private static readonly Dictionary<string, List<ResponseStreamContext>> ActiveResponseStreams = new();
    private static readonly object ResponseStreamsLock = new();

    public LoadViewGrpcService(
        ILoadViewService loadViewService,
        ILogger<LoadViewGrpcService> logger)
    {
        _loadViewService = loadViewService;
        _logger = logger;
    }

    private Guid GetUserId(ServerCallContext context)
    {
        var userIdClaim = context.GetHttpContext().User.FindFirst("sub")
            ?? context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "User not authenticated")));
    }

    // === Requester RPCs ===

    public override async Task<LoadViewRequest> CreateRequest(
        CreateLoadViewRequestRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new CreateLoadViewRequestDto
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters > 0 ? request.RadiusMeters : 1609,
            RequestType = request.RequestType,
            Description = request.Description,
            UrgencyLevel = MapUrgencyLevel(request.UrgencyLevel),
            ExpirationMinutes = request.ExpirationMinutes > 0 ? request.ExpirationMinutes : 30
        };

        var result = await _loadViewService.CreateRequestAsync(userId, dto);

        // Notify nearby streams about new request
        await NotifyNearbyStreamsAsync(result);

        return MapToProto(result);
    }

    public override async Task<LoadViewRequest> GetRequest(
        GetLoadViewRequestRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        var result = await _loadViewService.GetRequestAsync(requestId, userId);
        if (result == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Request not found"));

        return MapToProto(result);
    }

    public override async Task<GetMyRequestsResponse> GetMyRequests(
        Ondock.Api.Grpc.V1.Empty request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);
        var results = await _loadViewService.GetUserRequestsAsync(userId);

        var response = new GetMyRequestsResponse();
        response.Requests.AddRange(results.Select(MapToProto));
        return response;
    }

    public override async Task<Ondock.Api.Grpc.V1.Empty> CancelRequest(
        CancelLoadViewRequestRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        var success = await _loadViewService.CancelRequestAsync(requestId, userId);
        if (!success)
            throw new RpcException(new Status(StatusCode.NotFound, "Request not found or not owned by user"));

        // Notify streams that request was cancelled
        await NotifyRequestCancelledAsync(request.RequestId);

        return new Ondock.Api.Grpc.V1.Empty();
    }

    public override async Task<GetLoadViewResponsesResponse> GetResponses(
        GetLoadViewResponsesRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        var results = await _loadViewService.GetResponsesAsync(requestId);

        var response = new GetLoadViewResponsesResponse();
        response.Responses.AddRange(results.Select(MapResponseToProto));
        return response;
    }

    public override async Task<Ondock.Api.Grpc.V1.Empty> MarkResponseHelpful(
        MarkResponseHelpfulRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.ResponseId, out var responseId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid response ID"));

        var success = await _loadViewService.MarkResponseHelpfulAsync(responseId, userId, request.WasHelpful);
        if (!success)
            throw new RpcException(new Status(StatusCode.NotFound, "Response not found"));

        return new Ondock.Api.Grpc.V1.Empty();
    }

    // === Responder RPCs ===

    public override async Task<GetNearbyRequestsResponse> GetNearbyRequests(
        GetNearbyRequestsRequest request,
        ServerCallContext context)
    {
        var radiusMeters = request.RadiusMeters > 0 ? request.RadiusMeters : 10000;
        var result = await _loadViewService.GetNearbyRequestsAsync(
            request.Latitude,
            request.Longitude,
            radiusMeters);

        var response = new GetNearbyRequestsResponse
        {
            TotalCount = result.TotalCount
        };
        response.Requests.AddRange(result.Requests.Select(MapToProto));
        return response;
    }

    public override async Task<LoadViewResponse> SubmitResponse(
        SubmitLoadViewResponseRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        var dto = new SubmitLoadViewResponseDto
        {
            MediaUrl = request.MediaUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            MediaType = MapMediaType(request.MediaType),
            VideoDurationSeconds = request.VideoDurationSeconds,
            Caption = request.Caption
        };

        var result = await _loadViewService.SubmitResponseAsync(requestId, userId, dto);

        // Notify response streams about new response
        await NotifyResponseStreamsAsync(request.RequestId, result, LoadViewResponseEventType.ResponseEventNew);

        return MapResponseToProto(result);
    }

    public override async Task<StartLiveResponseResponse> StartLiveResponse(
        StartLiveResponseRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        var result = await _loadViewService.StartLiveResponseAsync(requestId, userId);

        // Notify response streams about live stream started
        var liveResponse = new LoadViewResponseDto
        {
            ResponseId = result.ResponseId,
            RequestId = requestId,
            ResponderId = userId,
            IsLiveStream = true,
            LiveKitRoomName = result.RoomName,
            CreatedAt = DateTime.UtcNow
        };
        await NotifyResponseStreamsAsync(request.RequestId, liveResponse, LoadViewResponseEventType.ResponseEventLiveStarted);

        return new StartLiveResponseResponse
        {
            ResponseId = result.ResponseId.ToString(),
            RoomName = result.RoomName,
            Token = result.Token,
            ServerUrl = result.ServerUrl
        };
    }

    public override async Task<Ondock.Api.Grpc.V1.Empty> EndLiveResponse(
        EndLiveResponseRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.ResponseId, out var responseId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid response ID"));

        var success = await _loadViewService.EndLiveResponseAsync(responseId, userId, request.RecordingUrl);
        if (!success)
            throw new RpcException(new Status(StatusCode.NotFound, "Response not found or not owned by user"));

        return new Ondock.Api.Grpc.V1.Empty();
    }

    public override async Task<Ondock.Api.Grpc.V1.Empty> DeleteResponse(
        DeleteLoadViewResponseRequest request,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.ResponseId, out var responseId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid response ID"));

        var success = await _loadViewService.DeleteResponseAsync(responseId, userId);
        if (!success)
            throw new RpcException(new Status(StatusCode.NotFound, "Response not found or not owned by user"));

        return new Ondock.Api.Grpc.V1.Empty();
    }

    // === Streaming RPCs ===

    public override async Task StreamNearbyRequests(
        StreamNearbyRequestsRequest request,
        IServerStreamWriter<LoadViewRequestEvent> responseStream,
        ServerCallContext context)
    {
        var userId = GetUserId(context);
        var radiusMeters = request.RadiusMeters > 0 ? request.RadiusMeters : 10000;

        _logger.LogInformation(
            "[LoadViewStream] User {UserId} starting nearby requests stream at ({Lat}, {Lng}), radius={Radius}m",
            userId, request.Latitude, request.Longitude, radiusMeters);

        var streamContext = new NearbyRequestStreamContext
        {
            UserId = userId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = radiusMeters,
            EventChannel = Channel.CreateUnbounded<LoadViewRequestEvent>()
        };

        lock (NearbyStreamsLock)
        {
            ActiveNearbyStreams[userId] = streamContext;
        }

        try
        {
            // Send initial batch of nearby requests
            var nearbyResult = await _loadViewService.GetNearbyRequestsAsync(
                request.Latitude,
                request.Longitude,
                radiusMeters);

            var initialEvent = new LoadViewRequestEvent
            {
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                EventType = LoadViewRequestEventType.RequestEventInitial
            };
            initialEvent.Requests.AddRange(nearbyResult.Requests.Select(MapToProto));
            await responseStream.WriteAsync(initialEvent);

            // Read from channel and write to stream
            await foreach (var evt in streamContext.EventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(evt);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[LoadViewStream] User {UserId} nearby stream cancelled", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoadViewStream] Error for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Stream error"));
        }
        finally
        {
            lock (NearbyStreamsLock)
            {
                ActiveNearbyStreams.Remove(userId);
            }
            _logger.LogInformation("[LoadViewStream] User {UserId} nearby stream ended", userId);
        }
    }

    public override async Task StreamResponses(
        StreamResponsesRequest request,
        IServerStreamWriter<LoadViewResponseEvent> responseStream,
        ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequestId, out var requestId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request ID"));

        _logger.LogInformation(
            "[LoadViewStream] User {UserId} starting response stream for request {RequestId}",
            userId, request.RequestId);

        var streamContext = new ResponseStreamContext
        {
            UserId = userId,
            RequestId = request.RequestId,
            EventChannel = Channel.CreateUnbounded<LoadViewResponseEvent>()
        };

        lock (ResponseStreamsLock)
        {
            if (!ActiveResponseStreams.ContainsKey(request.RequestId))
                ActiveResponseStreams[request.RequestId] = new List<ResponseStreamContext>();
            ActiveResponseStreams[request.RequestId].Add(streamContext);
        }

        try
        {
            // Send initial batch of responses
            var responses = await _loadViewService.GetResponsesAsync(requestId);

            var initialEvent = new LoadViewResponseEvent
            {
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                EventType = LoadViewResponseEventType.ResponseEventInitial
            };
            initialEvent.Responses.AddRange(responses.Select(MapResponseToProto));
            await responseStream.WriteAsync(initialEvent);

            // Read from channel and write to stream
            await foreach (var evt in streamContext.EventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(evt);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[LoadViewStream] User {UserId} response stream cancelled", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoadViewStream] Error for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Stream error"));
        }
        finally
        {
            lock (ResponseStreamsLock)
            {
                if (ActiveResponseStreams.TryGetValue(request.RequestId, out var contexts))
                {
                    contexts.Remove(streamContext);
                    if (contexts.Count == 0)
                        ActiveResponseStreams.Remove(request.RequestId);
                }
            }
            _logger.LogInformation("[LoadViewStream] User {UserId} response stream ended", userId);
        }
    }

    // === Notification Methods (called by service or background jobs) ===

    public static async Task NotifyNearbyStreamsAsync(LoadViewRequestDto request)
    {
        var evt = new LoadViewRequestEvent
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            EventType = LoadViewRequestEventType.RequestEventNew,
            Request = MapToProtoStatic(request)
        };

        List<NearbyRequestStreamContext> contextsToNotify;
        lock (NearbyStreamsLock)
        {
            contextsToNotify = ActiveNearbyStreams.Values
                .Where(ctx => IsWithinRadius(ctx, request.Latitude, request.Longitude, request.RadiusMeters))
                .ToList();
        }

        foreach (var ctx in contextsToNotify)
        {
            try
            {
                await ctx.EventChannel.Writer.WriteAsync(evt);
            }
            catch { /* Stream may be closed */ }
        }
    }

    public static async Task NotifyRequestCancelledAsync(string requestId)
    {
        var evt = new LoadViewRequestEvent
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            EventType = LoadViewRequestEventType.RequestEventCancelled,
            Request = new LoadViewRequest { RequestId = requestId }
        };

        List<NearbyRequestStreamContext> allStreams;
        lock (NearbyStreamsLock)
        {
            allStreams = ActiveNearbyStreams.Values.ToList();
        }

        foreach (var ctx in allStreams)
        {
            try
            {
                await ctx.EventChannel.Writer.WriteAsync(evt);
            }
            catch { /* Stream may be closed */ }
        }
    }

    public static async Task NotifyResponseStreamsAsync(string requestId, LoadViewResponseDto response, LoadViewResponseEventType eventType)
    {
        var evt = new LoadViewResponseEvent
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            EventType = eventType,
            Response = MapResponseToProtoStatic(response)
        };

        List<ResponseStreamContext> contextsToNotify;
        lock (ResponseStreamsLock)
        {
            if (!ActiveResponseStreams.TryGetValue(requestId, out var contexts))
                return;
            contextsToNotify = contexts.ToList();
        }

        foreach (var ctx in contextsToNotify)
        {
            try
            {
                await ctx.EventChannel.Writer.WriteAsync(evt);
            }
            catch { /* Stream may be closed */ }
        }
    }

    // === Helper Methods ===

    private static bool IsWithinRadius(NearbyRequestStreamContext ctx, double lat, double lng, int radiusMeters)
    {
        var distance = CalculateDistanceMeters(ctx.Latitude, ctx.Longitude, lat, lng);
        return distance <= Math.Max(ctx.RadiusMeters, radiusMeters);
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static UrgencyLevel MapUrgencyLevel(LoadViewUrgencyLevel level) => level switch
    {
        LoadViewUrgencyLevel.UrgencyLow => UrgencyLevel.Low,
        LoadViewUrgencyLevel.UrgencyMedium => UrgencyLevel.Medium,
        LoadViewUrgencyLevel.UrgencyHigh => UrgencyLevel.High,
        LoadViewUrgencyLevel.UrgencyCritical => UrgencyLevel.Critical,
        _ => UrgencyLevel.Medium
    };

    private static LoadViewUrgencyLevel MapUrgencyLevelToProto(UrgencyLevel level) => level switch
    {
        UrgencyLevel.Low => LoadViewUrgencyLevel.UrgencyLow,
        UrgencyLevel.Medium => LoadViewUrgencyLevel.UrgencyMedium,
        UrgencyLevel.High => LoadViewUrgencyLevel.UrgencyHigh,
        UrgencyLevel.Critical => LoadViewUrgencyLevel.UrgencyCritical,
        _ => LoadViewUrgencyLevel.UrgencyMedium
    };

    private static LoadViewRequestStatus MapStatusToProto(RequestStatus status) => status switch
    {
        RequestStatus.Active => LoadViewRequestStatus.RequestStatusActive,
        RequestStatus.Fulfilled => LoadViewRequestStatus.RequestStatusFulfilled,
        RequestStatus.Expired => LoadViewRequestStatus.RequestStatusExpired,
        _ => LoadViewRequestStatus.RequestStatusUnspecified
    };

    private static PostType MapMediaType(LoadViewMediaType mediaType) => mediaType switch
    {
        LoadViewMediaType.MediaTypeVideo => PostType.Video,
        LoadViewMediaType.MediaTypeImage => PostType.Photo,
        _ => PostType.Video
    };

    private static LoadViewMediaType MapMediaTypeToProto(PostType mediaType) => mediaType switch
    {
        PostType.Video => LoadViewMediaType.MediaTypeVideo,
        PostType.Photo => LoadViewMediaType.MediaTypeImage,
        _ => LoadViewMediaType.MediaTypeVideo
    };

    private LoadViewRequest MapToProto(LoadViewRequestDto dto) => MapToProtoStatic(dto);

    private static LoadViewRequest MapToProtoStatic(LoadViewRequestDto dto)
    {
        var proto = new LoadViewRequest
        {
            RequestId = dto.RequestId.ToString(),
            RequesterId = dto.RequesterId.ToString(),
            RequesterName = dto.RequesterName ?? "",
            RequesterProfilePicture = dto.RequesterProfilePicture ?? "",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            RadiusMeters = dto.RadiusMeters,
            RequestType = dto.RequestType ?? "",
            Description = dto.Description ?? "",
            UrgencyLevel = MapUrgencyLevelToProto(dto.UrgencyLevel),
            Status = MapStatusToProto(dto.Status),
            CreatedAt = Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            ExpiresAt = Timestamp.FromDateTime(dto.ExpiresAt.ToUniversalTime()),
            ResponseCount = dto.ResponseCount
        };

        if (dto.DistanceMeters.HasValue)
            proto.DistanceMeters = dto.DistanceMeters.Value;

        proto.Responses.AddRange(dto.Responses.Select(MapResponseToProtoStatic));
        return proto;
    }

    private LoadViewResponse MapResponseToProto(LoadViewResponseDto dto) => MapResponseToProtoStatic(dto);

    private static LoadViewResponse MapResponseToProtoStatic(LoadViewResponseDto dto)
    {
        var proto = new LoadViewResponse
        {
            ResponseId = dto.ResponseId.ToString(),
            RequestId = dto.RequestId.ToString(),
            ResponderId = dto.ResponderId.ToString(),
            ResponderName = dto.ResponderName ?? "",
            ResponderProfilePicture = dto.ResponderProfilePicture ?? "",
            MediaUrl = dto.MediaUrl ?? "",
            ThumbnailUrl = dto.ThumbnailUrl ?? "",
            MediaType = MapMediaTypeToProto(dto.MediaType),
            VideoDurationSeconds = dto.VideoDurationSeconds,
            Caption = dto.Caption ?? "",
            CreatedAt = Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            IsLiveStream = dto.IsLiveStream,
            LiveKitRoomName = dto.LiveKitRoomName ?? "",
            RewardPoints = dto.RewardPoints
        };

        if (dto.WasHelpful.HasValue)
            proto.WasHelpful = dto.WasHelpful.Value;

        return proto;
    }

    // === Stream Context Classes ===

    private class NearbyRequestStreamContext
    {
        public Guid UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; }
        public Channel<LoadViewRequestEvent> EventChannel { get; set; } = null!;
    }

    private class ResponseStreamContext
    {
        public Guid UserId { get; set; }
        public string RequestId { get; set; } = "";
        public Channel<LoadViewResponseEvent> EventChannel { get; set; } = null!;
    }
}
