using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcMonitoringSessionResponse = Ondock.Api.Grpc.V1.MonitoringSessionResponse;
using GrpcLiveKitTokenResponse = Ondock.Api.Grpc.V1.LiveKitTokenResponse;
using GrpcStartSessionRequest = Ondock.Api.Grpc.V1.StartSessionRequest;
using GrpcEndSessionRequest = Ondock.Api.Grpc.V1.EndSessionRequest;
using GrpcGetSessionsRequest = Ondock.Api.Grpc.V1.GetSessionsRequest;
using GrpcGetSessionsResponse = Ondock.Api.Grpc.V1.GetSessionsResponse;
using GrpcJoinSessionRequest = Ondock.Api.Grpc.V1.JoinSessionRequest;
using GrpcUpdateStatusRequest = Ondock.Api.Grpc.V1.UpdateStatusRequest;
using GrpcRequestStreamRequest = Ondock.Api.Grpc.V1.RequestStreamRequest;
using GrpcUpdateStreamStateRequest = Ondock.Api.Grpc.V1.UpdateStreamStateRequest;
using GrpcGetTokenRequest = Ondock.Api.Grpc.V1.GetTokenRequest;
using DtoStartMonitoringSessionRequest = ondock.api.DTOs.Monitoring.StartMonitoringSessionRequest;
using DtoJoinMonitoringSessionRequest = ondock.api.DTOs.Monitoring.JoinMonitoringSessionRequest;
using DtoMonitoringStatusRequest = ondock.api.DTOs.Monitoring.MonitoringStatusRequest;
using DtoRequestStreamRequest = ondock.api.DTOs.Monitoring.RequestStreamRequest;
using DtoUpdateStreamStateRequest = ondock.api.DTOs.Monitoring.UpdateStreamStateRequest;
using DtoMonitoringSessionResponse = ondock.api.DTOs.Monitoring.MonitoringSessionResponse;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class MonitoringGrpcService : MonitoringService.MonitoringServiceBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringGrpcService> _logger;

    // Track active monitoring stream subscriptions: SessionId -> (UserId -> Channel)
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<MonitoringEvent>>> SessionStreams = new();

    public MonitoringGrpcService(IMonitoringService monitoringService, ILogger<MonitoringGrpcService> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    public override async Task<GrpcMonitoringSessionResponse> StartSession(GrpcStartSessionRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoStartMonitoringSessionRequest
        {
            InitiatorDeviceId = "grpc-" + userId.ToString("N").Substring(0, 8),
            Metadata = request.SessionName // Use SessionName as metadata
        };

        var result = await _monitoringService.StartSessionAsync(userId, dto);

        return MapSessionResponse(result);
    }

    public override async Task<GrpcMonitoringSessionResponse> EndSession(GrpcEndSessionRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var result = await _monitoringService.EndSessionAsync(userId, sessionId);

        // Broadcast session ended to all participants
        await BroadcastSessionEnded(sessionId);

        return MapSessionResponse(result);
    }

    public override async Task<GrpcGetSessionsResponse> GetSessions(GrpcGetSessionsRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var take = request.Take > 0 ? request.Take : 25;

        var sessions = await _monitoringService.GetSessionsAsync(userId, take);

        var response = new GrpcGetSessionsResponse();
        foreach (var session in sessions)
        {
            response.Sessions.Add(MapSessionResponse(session));
        }

        return response;
    }

    public override async Task<GrpcEmpty> JoinSession(GrpcJoinSessionRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var dto = new DtoJoinMonitoringSessionRequest
        {
            DeviceId = request.DeviceId
        };

        await _monitoringService.JoinSessionAsync(userId, sessionId, dto);

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> UpdateStatus(GrpcUpdateStatusRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var dto = new DtoMonitoringStatusRequest
        {
            DeviceId = request.SessionId ?? "", // Use SessionId as DeviceId
            Status = 0 // Default status
        };

        await _monitoringService.UpdateStatusAsync(userId, dto);

        // Broadcast status update to session participants
        await BroadcastParticipantStatus(sessionId, userId, dto);

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> RequestStream(GrpcRequestStreamRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var dto = new DtoRequestStreamRequest
        {
            DeviceId = request.SessionId ?? "" // Use SessionId as DeviceId
        };

        await _monitoringService.RequestStreamAsync(userId, sessionId, dto);

        // Broadcast stream request
        await BroadcastStreamRequest(sessionId, userId, request.TargetParticipantId ?? "", request.StreamType ?? "");

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> UpdateStreamState(GrpcUpdateStreamStateRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var dto = new DtoUpdateStreamStateRequest
        {
            IsStreaming = request.IsStreaming
        };

        await _monitoringService.UpdateStreamStateAsync(userId, sessionId, dto);

        // Broadcast stream state change
        await BroadcastStreamStateChanged(sessionId, userId, request.IsStreaming, request.StreamType ?? "");

        return new GrpcEmpty();
    }

    public override async Task<GrpcLiveKitTokenResponse> GetToken(GrpcGetTokenRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        var result = await _monitoringService.GetLiveKitTokenAsync(userId, sessionId, request.DeviceId);

        return new GrpcLiveKitTokenResponse
        {
            Token = result.Token,
            RoomName = "", // Not in DTO
            ServerUrl = "", // Not in DTO
            ExpiresAt = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)) // Default expiry
        };
    }

    // === STREAMING RPC - Real-time monitoring events ===
    public override async Task StreamMonitoringEvents(StreamMonitoringEventsRequest request, IServerStreamWriter<MonitoringEvent> responseStream, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid session ID"));
        }

        _logger.LogInformation("gRPC: User {UserId} started streaming monitoring events for session {SessionId}", userId, sessionId);

        var channel = Channel.CreateUnbounded<MonitoringEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Get or create session streams dictionary
        var sessionChannels = SessionStreams.GetOrAdd(sessionId, _ => new ConcurrentDictionary<Guid, Channel<MonitoringEvent>>());
        sessionChannels[userId] = channel;

        try
        {
            await foreach (var monitoringEvent in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(monitoringEvent);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("gRPC: User {UserId} disconnected from monitoring stream for session {SessionId}", userId, sessionId);
        }
        finally
        {
            sessionChannels.TryRemove(userId, out _);
            if (sessionChannels.IsEmpty)
            {
                SessionStreams.TryRemove(sessionId, out _);
            }
            channel.Writer.Complete();
        }
    }

    // === Broadcast helpers ===

    private async Task BroadcastToSession(Guid sessionId, MonitoringEvent monitoringEvent, Guid? excludeUserId = null)
    {
        if (!SessionStreams.TryGetValue(sessionId, out var sessionChannels))
            return;

        foreach (var (userId, channel) in sessionChannels)
        {
            if (excludeUserId.HasValue && userId == excludeUserId.Value)
                continue;

            await channel.Writer.WriteAsync(monitoringEvent);
        }
    }

    private async Task BroadcastParticipantStatus(Guid sessionId, Guid userId, DtoMonitoringStatusRequest status)
    {
        var monitoringEvent = new MonitoringEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            SessionId = sessionId.ToString(),
            ParticipantStatus = new ParticipantStatusEvent
            {
                ParticipantId = userId.ToString(),
                StatusMessage = $"Status: {status.Status}"
            }
        };

        await BroadcastToSession(sessionId, monitoringEvent, userId);
    }

    private async Task BroadcastStreamRequest(Guid sessionId, Guid requesterId, string targetParticipantId, string streamType)
    {
        var monitoringEvent = new MonitoringEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            SessionId = sessionId.ToString(),
            StreamRequest = new StreamRequestEvent
            {
                RequesterId = requesterId.ToString(),
                TargetParticipantId = targetParticipantId,
                StreamType = streamType
            }
        };

        await BroadcastToSession(sessionId, monitoringEvent);
    }

    private async Task BroadcastStreamStateChanged(Guid sessionId, Guid userId, bool isStreaming, string streamType)
    {
        var monitoringEvent = new MonitoringEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            SessionId = sessionId.ToString(),
            StreamStateChanged = new StreamStateChangedEvent
            {
                ParticipantId = userId.ToString(),
                IsStreaming = isStreaming,
                StreamType = streamType
            }
        };

        await BroadcastToSession(sessionId, monitoringEvent, userId);
    }

    private async Task BroadcastSessionEnded(Guid sessionId)
    {
        var monitoringEvent = new MonitoringEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            SessionId = sessionId.ToString(),
            SessionEnded = new SessionEndedEvent
            {
                Reason = "Session ended by owner"
            }
        };

        await BroadcastToSession(sessionId, monitoringEvent);
    }

    private static GrpcMonitoringSessionResponse MapSessionResponse(DtoMonitoringSessionResponse result)
    {
        var response = new GrpcMonitoringSessionResponse
        {
            SessionId = result.SessionId.ToString(),
            SessionName = result.Metadata ?? "Monitoring Session",
            Description = "",
            Status = result.IsStreaming ? "active" : "inactive",
            OwnerId = result.UserId.ToString(),
            CreatedAt = Timestamp.FromDateTime(result.StartedAt.ToUniversalTime()),
            ParticipantCount = result.Participants.Count,
            RoomName = result.InitiatorDeviceId ?? ""
        };

        if (result.EndedAt.HasValue)
        {
            response.EndedAt = Timestamp.FromDateTime(result.EndedAt.Value.ToUniversalTime());
        }

        return response;
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
}
