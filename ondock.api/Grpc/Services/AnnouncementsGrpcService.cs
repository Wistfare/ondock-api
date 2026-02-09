using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcAnnouncement = Ondock.Api.Grpc.V1.Announcement;
using DtoAnnouncementDto = ondock.api.DTOs.Chat.AnnouncementDto;
using DtoCreateAnnouncementRequest = ondock.api.DTOs.Chat.CreateAnnouncementRequest;
using DtoCreateAnnouncementResponse = ondock.api.DTOs.Chat.CreateAnnouncementResponse;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class AnnouncementsGrpcService : AnnouncementsService.AnnouncementsServiceBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly ILogger<AnnouncementsGrpcService> _logger;

    // Track active announcement stream subscriptions
    private static readonly ConcurrentDictionary<Guid, Channel<AnnouncementEvent>> ActiveStreams = new();

    public AnnouncementsGrpcService(IAnnouncementService announcementService, ILogger<AnnouncementsGrpcService> logger)
    {
        _announcementService = announcementService;
        _logger = logger;
    }

    public override async Task<GetAnnouncementsResponse> GetAnnouncements(GetAnnouncementsRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var take = request.Take > 0 ? request.Take : 50;

        var announcements = await _announcementService.GetAnnouncementsAsync(
            userId,
            request.Latitude,
            request.Longitude,
            take);

        var response = new GetAnnouncementsResponse();

        foreach (var ann in announcements)
        {
            response.Announcements.Add(MapAnnouncement(ann));
        }

        return response;
    }

    public override async Task<Announcement> GetAnnouncement(GetAnnouncementRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AnnouncementId, out var announcementId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid announcement ID"));
        }

        var announcement = await _announcementService.GetAnnouncementAsync(announcementId);

        if (announcement == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Announcement not found"));
        }

        return MapAnnouncement(announcement);
    }

    public override async Task<CreateAnnouncementResponse> CreateAnnouncement(CreateAnnouncementRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoCreateAnnouncementRequest
        {
            Title = request.Title,
            Body = request.Body,
            UrgencyLevel = (ondock.api.Data.Entities.UrgencyLevel)request.UrgencyLevel,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedMph = 0 // Default value
        };

        var result = await _announcementService.CreateAnnouncementAsync(userId, dto);

        // Broadcast to all active announcement streams
        var response = new CreateAnnouncementResponse
        {
            AnnouncementId = result.AnnouncementId,
            Message = "Announcement created successfully"
        };
        
        // Push to all users listening on announcement streams
        _ = BroadcastAnnouncementCreated(response);

        return response;
    }

    public override async Task<GrpcEmpty> DeleteAnnouncement(DeleteAnnouncementRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.AnnouncementId, out var announcementId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid announcement ID"));
        }

        var deleted = await _announcementService.DeleteAnnouncementAsync(userId, announcementId);

        if (!deleted)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Announcement not found or not owned by user"));
        }

        // Broadcast deletion
        await BroadcastAnnouncementDeleted(request.AnnouncementId);

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> TrackView(TrackViewRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.AnnouncementId, out var announcementId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid announcement ID"));
        }

        await _announcementService.IncrementViewCountAsync(announcementId, userId);

        return new GrpcEmpty();
    }

    // === STREAMING RPC - Real-time announcement events ===
    public override async Task StreamAnnouncements(StreamAnnouncementsRequest request, IServerStreamWriter<AnnouncementEvent> responseStream, ServerCallContext context)
    {
        var userId = GetUserId(context);
        _logger.LogInformation("gRPC: User {UserId} started streaming announcements", userId);

        var channel = Channel.CreateUnbounded<AnnouncementEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        ActiveStreams[userId] = channel;

        try
        {
            await foreach (var announcementEvent in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                // Filter by distance if radius specified
                if (request.RadiusMiles > 0 && announcementEvent.Created != null)
                {
                    var ann = announcementEvent.Created.Announcement;
                    if (ann.DistanceMiles > request.RadiusMiles)
                        continue;
                }

                await responseStream.WriteAsync(announcementEvent);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("gRPC: User {UserId} disconnected from announcements stream", userId);
        }
        finally
        {
            ActiveStreams.TryRemove(userId, out _);
            channel.Writer.Complete();
        }
    }

    // === Broadcast helpers ===

    private async Task BroadcastAnnouncementCreated(CreateAnnouncementResponse result)
    {
        // Get the full announcement details
        if (!Guid.TryParse(result.AnnouncementId, out var announcementId))
            return;

        var announcement = await _announcementService.GetAnnouncementAsync(announcementId);
        if (announcement == null)
            return;

        var announcementEvent = new AnnouncementEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            Created = new AnnouncementCreatedEvent
            {
                Announcement = MapAnnouncement(announcement)
            }
        };

        // Broadcast to all active streams
        foreach (var (_, channel) in ActiveStreams)
        {
            await channel.Writer.WriteAsync(announcementEvent);
        }
    }

    private async Task BroadcastAnnouncementDeleted(string announcementId)
    {
        var announcementEvent = new AnnouncementEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            Deleted = new AnnouncementDeletedEvent
            {
                AnnouncementId = announcementId
            }
        };

        foreach (var (_, channel) in ActiveStreams)
        {
            await channel.Writer.WriteAsync(announcementEvent);
        }
    }

    // Static method for external services to push events
    public static async Task PushEventToAllUsers(AnnouncementEvent announcementEvent)
    {
        foreach (var (_, channel) in ActiveStreams)
        {
            await channel.Writer.WriteAsync(announcementEvent);
        }
    }

    private static GrpcAnnouncement MapAnnouncement(DtoAnnouncementDto dto)
    {
        return new GrpcAnnouncement
        {
            AnnouncementId = dto.AnnouncementId.ToString(),
            Title = dto.Title ?? "",
            Body = dto.Body ?? "",
            UrgencyLevel = (int)dto.UrgencyLevel,
            AuthorAnonymousId = dto.AuthorAnonymousId ?? "",
            AuthorDisplayName = dto.AuthorDisplayName ?? "",
            AuthorColor = dto.AuthorColor ?? "",
            IsMine = dto.IsMine,
            CreatedAt = Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            DistanceMiles = dto.DistanceMiles,
            ViewCount = dto.ViewCount,
            ReplyCount = dto.ReplyCount
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
}
