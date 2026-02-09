using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using GrpcEmpty = Google.Protobuf.WellKnownTypes.Empty;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.DTOs.Posts;
using ondock.api.Services.Interfaces;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class UserStatusGrpcService : StatusService.StatusServiceBase
{
    private readonly IUserStatusService _statusService;
    private readonly ILogger<UserStatusGrpcService> _logger;

    public UserStatusGrpcService(
        IUserStatusService statusService,
        ILogger<UserStatusGrpcService> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    public override async Task<CreateStatusResponse> CreateStatus(CreateStatusRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new CreatePostRequest
        {
            PostType = MapPostType(request.PostType),
            TextContent = request.HasTextContent ? request.TextContent : null,
            BackgroundColor = request.HasBackgroundColor ? request.BackgroundColor : null,
            BackgroundImageUrl = request.HasBackgroundImageUrl ? request.BackgroundImageUrl : null,
            TextPositionX = request.HasTextPositionX ? request.TextPositionX : null,
            TextPositionY = request.HasTextPositionY ? request.TextPositionY : null,
            MediaUrl = request.HasMediaUrl ? request.MediaUrl : null,
            ThumbnailUrl = request.HasThumbnailUrl ? request.ThumbnailUrl : null,
            DurationSeconds = request.HasDurationSeconds ? request.DurationSeconds : null
        };

        var result = await _statusService.CreateStatusAsync(userId, dto);

        return new CreateStatusResponse
        {
            Status = MapToGrpcStatus(result)
        };
    }

    public override async Task<GetMyStatusesResponse> GetMyStatuses(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var statuses = await _statusService.GetMyStatusesAsync(userId);

        var response = new GetMyStatusesResponse();
        response.Statuses.AddRange(statuses.Select(MapToGrpcStatus));
        return response;
    }

    public override async Task<GetUserStatusesResponse> GetUserStatuses(GetUserStatusesRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var result = await _statusService.GetUserStatusesAsync(userId, request.AnonymousUserId);

        var response = new GetUserStatusesResponse
        {
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Color = result.Color,
            PostCount = result.PostCount
        };

        if (result.LastPostAt.HasValue)
        {
            response.LastPostAt = Timestamp.FromDateTime(result.LastPostAt.Value.ToUniversalTime());
        }

        response.Statuses.AddRange(result.Posts.Select(MapToGrpcStatus));
        return response;
    }

    public override async Task<GetNearbyUsersWithStatusesResponse> GetNearbyUsersWithStatuses(
        GetNearbyUsersWithStatusesRequest request, 
        ServerCallContext context)
    {
        var userId = GetUserId(context);
        var radiusMiles = request.HasRadiusMiles ? request.RadiusMiles : 10.0;

        var users = await _statusService.GetNearbyUsersWithStatusesAsync(
            userId, request.Latitude, request.Longitude, radiusMiles);

        var response = new GetNearbyUsersWithStatusesResponse();
        response.Users.AddRange(users.Select(u => new NearbyUserWithStatuses
        {
            AnonymousUserId = u.AnonymousUserId,
            DisplayName = u.DisplayName,
            Color = u.Color,
            Latitude = u.Latitude,
            Longitude = u.Longitude,
            DistanceMiles = u.DistanceMiles,
            PostCount = u.PostCount,
            HasUnseenPosts = u.HasUnseenPosts
        }));

        return response;
    }

    public override async Task<GrpcEmpty> DeleteStatus(DeleteStatusRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        
        if (!Guid.TryParse(request.PostId, out var postId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid post ID"));
        }

        var success = await _statusService.DeleteStatusAsync(userId, postId);
        if (!success)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Status not found or not owned by user"));
        }

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> RecordStatusView(RecordStatusViewRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.PostId, out var postId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid post ID"));
        }

        await _statusService.RecordViewAsync(userId, postId);
        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> RecordStatusViewsBatch(RecordStatusViewsBatchRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (request.PostIds.Count == 0)
        {
            return new GrpcEmpty();
        }

        var postIds = new List<Guid>();
        foreach (var postIdStr in request.PostIds)
        {
            if (Guid.TryParse(postIdStr, out var postId))
            {
                postIds.Add(postId);
            }
        }

        if (postIds.Count > 0)
        {
            await _statusService.RecordViewsBatchAsync(userId, postIds);
        }

        return new GrpcEmpty();
    }

    private static UserStatus MapToGrpcStatus(PostDto dto)
    {
        var status = new UserStatus
        {
            PostId = dto.PostId,
            UserId = dto.UserId,
            AuthorDisplayName = dto.AuthorDisplayName,
            AuthorColor = dto.AuthorColor,
            PostType = MapToGrpcPostType(dto.PostType),
            CreatedAt = Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            ExpiresAt = Timestamp.FromDateTime(dto.ExpiresAt.ToUniversalTime()),
            ViewCount = dto.ViewCount,
            IsMine = dto.IsMine
        };

        if (dto.TextContent != null) status.TextContent = dto.TextContent;
        if (dto.BackgroundColor != null) status.BackgroundColor = dto.BackgroundColor;
        if (dto.BackgroundImageUrl != null) status.BackgroundImageUrl = dto.BackgroundImageUrl;
        if (dto.TextPositionX.HasValue) status.TextPositionX = dto.TextPositionX.Value;
        if (dto.TextPositionY.HasValue) status.TextPositionY = dto.TextPositionY.Value;
        if (dto.MediaUrl != null) status.MediaUrl = dto.MediaUrl;
        if (dto.ThumbnailUrl != null) status.ThumbnailUrl = dto.ThumbnailUrl;
        if (dto.DurationSeconds.HasValue) status.DurationSeconds = dto.DurationSeconds.Value;

        return status;
    }

    private static Data.Entities.StatusPostType MapPostType(Ondock.Api.Grpc.V1.StatusPostType type)
    {
        return type switch
        {
            Ondock.Api.Grpc.V1.StatusPostType.Text => Data.Entities.StatusPostType.Text,
            Ondock.Api.Grpc.V1.StatusPostType.Image => Data.Entities.StatusPostType.Image,
            Ondock.Api.Grpc.V1.StatusPostType.Video => Data.Entities.StatusPostType.Video,
            _ => Data.Entities.StatusPostType.Text
        };
    }

    private static Ondock.Api.Grpc.V1.StatusPostType MapToGrpcPostType(Data.Entities.StatusPostType type)
    {
        return type switch
        {
            Data.Entities.StatusPostType.Text => Ondock.Api.Grpc.V1.StatusPostType.Text,
            Data.Entities.StatusPostType.Image => Ondock.Api.Grpc.V1.StatusPostType.Image,
            Data.Entities.StatusPostType.Video => Ondock.Api.Grpc.V1.StatusPostType.Video,
            _ => Ondock.Api.Grpc.V1.StatusPostType.Text
        };
    }

    private Guid GetUserId(ServerCallContext context)
    {
        var userIdClaim = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid authentication token"));
        }
        return userId;
    }

    // ============ STATUS STREAMING ============

    private class StatusStreamContext
    {
        public Channel<NearbyUsersWithStatusesEvent> EventChannel { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusMiles { get; set; }
    }

    private static readonly ConcurrentDictionary<Guid, StatusStreamContext> ActiveStatusStreams = new();

    public override async Task StreamNearbyUsersWithStatuses(
        StreamNearbyUsersWithStatusesRequest request,
        IServerStreamWriter<NearbyUsersWithStatusesEvent> responseStream,
        ServerCallContext context)
    {
        var userId = GetUserId(context);
        var radiusMiles = request.HasRadiusMiles ? request.RadiusMiles : 10.0;

        _logger.LogInformation(
            "[StatusStream] User {UserId} starting stream at ({Lat}, {Lng}), radius={Radius}mi",
            userId, request.Latitude, request.Longitude, radiusMiles);

        var streamContext = new StatusStreamContext
        {
            EventChannel = Channel.CreateUnbounded<NearbyUsersWithStatusesEvent>(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMiles = radiusMiles
        };

        ActiveStatusStreams[userId] = streamContext;

        try
        {
            // Send initial batch
            var initialEvent = await BuildStatusEventAsync(userId, streamContext, NearbyUsersWithStatusesEventType.Initial);
            await responseStream.WriteAsync(initialEvent);

            // Periodic refresh task (every 30 seconds)
            var refreshCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            _ = Task.Run(async () =>
            {
                while (!refreshCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), refreshCts.Token);
                    try
                    {
                        var refreshEvent = await BuildStatusEventAsync(userId, streamContext, NearbyUsersWithStatusesEventType.UserUpdated);
                        await streamContext.EventChannel.Writer.WriteAsync(refreshEvent, refreshCts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[StatusStream] Error during refresh for user {UserId}", userId);
                    }
                }
            }, refreshCts.Token);

            // Read from channel and write to stream
            await foreach (var evt in streamContext.EventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(evt);
            }

            refreshCts.Cancel();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[StatusStream] User {UserId} stream cancelled", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StatusStream] Error for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Stream error"));
        }
        finally
        {
            ActiveStatusStreams.TryRemove(userId, out _);
            _logger.LogInformation("[StatusStream] User {UserId} stream ended", userId);
        }
    }

    private async Task<NearbyUsersWithStatusesEvent> BuildStatusEventAsync(
        Guid userId, StatusStreamContext ctx, NearbyUsersWithStatusesEventType eventType)
    {
        var users = await _statusService.GetNearbyUsersWithStatusesAsync(
            userId, ctx.Latitude, ctx.Longitude, ctx.RadiusMiles);

        var evt = new NearbyUsersWithStatusesEvent
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            EventType = eventType
        };

        evt.Users.AddRange(users.Select(u => new NearbyUserWithStatuses
        {
            AnonymousUserId = u.AnonymousUserId,
            DisplayName = u.DisplayName,
            Color = u.Color,
            Latitude = u.Latitude,
            Longitude = u.Longitude,
            DistanceMiles = u.DistanceMiles,
            PostCount = u.PostCount,
            HasUnseenPosts = u.HasUnseenPosts
        }));

        return evt;
    }
}
