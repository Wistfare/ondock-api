using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.LoadView;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class LoadViewService : ILoadViewService
{
    private readonly OnDockDbContext _db;
    private readonly ILiveKitTokenService _liveKitService;
    private readonly ILoadViewRealtimeService _realtimeService;
    private readonly IFcmNotificationService _fcmService;
    private readonly ILogger<LoadViewService> _logger;
    private readonly GeometryFactory _geometryFactory;

    public LoadViewService(
        OnDockDbContext db,
        ILiveKitTokenService liveKitService,
        ILoadViewRealtimeService realtimeService,
        IFcmNotificationService fcmService,
        ILogger<LoadViewService> logger)
    {
        _db = db;
        _liveKitService = liveKitService;
        _realtimeService = realtimeService;
        _fcmService = fcmService;
        _logger = logger;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task<LoadViewRequestDto> CreateRequestAsync(Guid userId, CreateLoadViewRequestDto dto)
    {
        var location = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var request = new RoadRequest
        {
            RequestId = Guid.NewGuid(),
            RequesterId = userId,
            Location = location,
            Radius = dto.RadiusMeters,
            RequestType = dto.RequestType,
            Description = dto.Description,
            UrgencyLevel = dto.UrgencyLevel,
            Status = RequestStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(dto.ExpirationMinutes),
            ResponseCount = 0
        };

        _db.RoadRequests.Add(request);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        var result = MapToDto(request, user);

        // Find nearby users and push notifications directly to them
        await NotifyNearbyUsersDirectlyAsync(userId, location, dto.RadiusMeters, result);

        return result;
    }

    private async Task NotifyNearbyUsersDirectlyAsync(Guid requesterId, Point location, int radiusMeters, LoadViewRequestDto request)
    {
        try
        {
            // Query users within radius based on their current location
            var nearbyUsers = await _db.UserLocations
                .Where(ul => ul.UserId != requesterId)
                .Where(ul => ul.Timestamp >= DateTime.UtcNow.AddMinutes(-30)) // Only active users
                .Select(ul => new { ul.UserId, ul.CurrentLocation })
                .ToListAsync();

            // Filter by distance and send push notifications
            foreach (var nearbyUser in nearbyUsers)
            {
                var distanceMeters = nearbyUser.CurrentLocation.Distance(location);
                if (distanceMeters <= radiusMeters)
                {
                    // Send push notification
                    await _fcmService.SendLoadViewRequestNearbyAsync(
                        nearbyUser.UserId, 
                        request, 
                        distanceMeters);

                    // Also send SignalR notification
                    await _realtimeService.NotifyNearbyUsersAsync(
                        nearbyUser.CurrentLocation.Y, 
                        nearbyUser.CurrentLocation.X, 
                        request);
                }
            }

            _logger.LogInformation("Notified {Count} nearby users about LoadView request {RequestId}", 
                nearbyUsers.Count(u => u.CurrentLocation.Distance(location) <= radiusMeters), 
                request.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying nearby users for LoadView request {RequestId}", request.RequestId);
        }
    }

    public async Task<LoadViewRequestDto?> GetRequestAsync(Guid requestId, Guid? userId = null)
    {
        var request = await _db.RoadRequests
            .Include(r => r.Requester)
            .Include(r => r.Responses)
                .ThenInclude(resp => resp.Responder)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return null;

        return MapToDto(request, request.Requester, includeResponses: true);
    }

    public async Task<List<LoadViewRequestDto>> GetUserRequestsAsync(Guid userId)
    {
        var requests = await _db.RoadRequests
            .Include(r => r.Requester)
            .Where(r => r.RequesterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => MapToDto(r, r.Requester)).ToList();
    }

    public async Task<NearbyLoadViewRequestsDto> GetNearbyRequestsAsync(double latitude, double longitude, int radiusMeters = 10000)
    {
        var userLocation = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        var requests = await _db.RoadRequests
            .Include(r => r.Requester)
            .Where(r => r.Status == RequestStatus.Active && r.ExpiresAt > DateTime.UtcNow)
            .Where(r => r.Location.Distance(userLocation) <= radiusMeters)
            .OrderBy(r => r.Location.Distance(userLocation))
            .Take(50)
            .ToListAsync();

        var dtos = requests.Select(r =>
        {
            var dto = MapToDto(r, r.Requester);
            dto.DistanceMeters = r.Location.Distance(userLocation);
            return dto;
        }).ToList();

        return new NearbyLoadViewRequestsDto
        {
            UserLatitude = latitude,
            UserLongitude = longitude,
            TotalCount = dtos.Count,
            Requests = dtos
        };
    }

    public async Task<bool> CancelRequestAsync(Guid requestId, Guid userId)
    {
        var request = await _db.RoadRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId && r.RequesterId == userId);

        if (request == null) return false;

        request.Status = RequestStatus.Expired;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<LoadViewResponseDto> SubmitResponseAsync(Guid requestId, Guid userId, SubmitLoadViewResponseDto dto)
    {
        var request = await _db.RoadRequests.FindAsync(requestId);
        if (request == null || request.Status != RequestStatus.Active)
            throw new InvalidOperationException("Request not found or not active");

        var response = new RoadRequestResponse
        {
            ResponseId = Guid.NewGuid(),
            RequestId = requestId,
            ResponderId = userId,
            MediaUrl = dto.MediaUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            MediaType = dto.MediaType,
            VideoDurationSeconds = dto.VideoDurationSeconds,
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow,
            IsLiveStream = false,
            RewardPoints = CalculateRewardPoints(request)
        };

        _db.RoadRequestResponses.Add(response);
        request.ResponseCount++;
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        var result = MapResponseToDto(response, user);

        // Notify requester about the new response (SignalR + Push)
        await _realtimeService.NotifyNewResponseAsync(requestId, result);
        await _fcmService.SendLoadViewResponseReceivedAsync(request.RequesterId, result);

        return result;
    }

    public async Task<StartLiveResponseDto> StartLiveResponseAsync(Guid requestId, Guid userId)
    {
        var request = await _db.RoadRequests.FindAsync(requestId);
        if (request == null || request.Status != RequestStatus.Active)
            throw new InvalidOperationException("Request not found or not active");

        var roomName = $"loadview_{requestId}_{Guid.NewGuid():N}";
        var token = _liveKitService.GenerateToken(roomName, userId.ToString(), canPublish: true, canSubscribe: true);

        var response = new RoadRequestResponse
        {
            ResponseId = Guid.NewGuid(),
            RequestId = requestId,
            ResponderId = userId,
            CreatedAt = DateTime.UtcNow,
            IsLiveStream = true,
            LiveKitRoomName = roomName,
            MediaType = PostType.Video,
            RewardPoints = CalculateRewardPoints(request)
        };

        _db.RoadRequestResponses.Add(response);
        request.ResponseCount++;
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        var responseDto = MapResponseToDto(response, user);

        // Notify requester about the live stream (SignalR + Push)
        await _realtimeService.NotifyLiveStreamStartedAsync(requestId, responseDto);
        await _fcmService.SendLoadViewLiveStreamStartedAsync(request.RequesterId, responseDto);

        return new StartLiveResponseDto
        {
            ResponseId = response.ResponseId,
            RoomName = roomName,
            Token = token,
            ServerUrl = Environment.GetEnvironmentVariable("LIVEKIT_URL") ?? "wss://livekit.ondock.app"
        };
    }

    public async Task<bool> EndLiveResponseAsync(Guid responseId, Guid userId, string? recordingUrl)
    {
        var response = await _db.RoadRequestResponses
            .FirstOrDefaultAsync(r => r.ResponseId == responseId && r.ResponderId == userId);

        if (response == null) return false;

        if (!string.IsNullOrEmpty(recordingUrl))
        {
            response.MediaUrl = recordingUrl;
        }

        await _db.SaveChangesAsync();

        // Notify that live stream ended
        await _realtimeService.NotifyLiveStreamEndedAsync(response.RequestId, responseId);

        return true;
    }

    public async Task<List<LoadViewResponseDto>> GetResponsesAsync(Guid requestId)
    {
        var responses = await _db.RoadRequestResponses
            .Include(r => r.Responder)
            .Where(r => r.RequestId == requestId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return responses.Select(r => MapResponseToDto(r, r.Responder)).ToList();
    }

    public async Task<bool> MarkResponseHelpfulAsync(Guid responseId, Guid userId, bool wasHelpful)
    {
        var response = await _db.RoadRequestResponses
            .Include(r => r.Request)
            .FirstOrDefaultAsync(r => r.ResponseId == responseId);

        if (response == null) return false;

        // Only the requester can mark as helpful
        if (response.Request.RequesterId != userId) return false;

        response.WasHelpful = wasHelpful;

        // Award bonus points if marked helpful
        if (wasHelpful)
        {
            response.RewardPoints += 10;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteResponseAsync(Guid responseId, Guid userId)
    {
        var response = await _db.RoadRequestResponses
            .Include(r => r.Request)
            .FirstOrDefaultAsync(r => r.ResponseId == responseId && r.ResponderId == userId);

        if (response == null) return false;

        response.Request.ResponseCount--;
        _db.RoadRequestResponses.Remove(response);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task ExpireOldRequestsAsync(CancellationToken cancellationToken = default)
    {
        var expiredRequests = await _db.RoadRequests
            .Where(r => r.Status == RequestStatus.Active && r.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var request in expiredRequests)
        {
            request.Status = RequestStatus.Expired;
        }

        if (expiredRequests.Any())
        {
            await _db.SaveChangesAsync(cancellationToken);

            // Notify subscribers that requests have expired
            foreach (var request in expiredRequests)
            {
                await _realtimeService.NotifyRequestExpiredAsync(request.RequestId);
            }
            _logger.LogInformation("Expired {Count} LoadView requests", expiredRequests.Count);
        }
    }

    private static int CalculateRewardPoints(RoadRequest request)
    {
        return request.UrgencyLevel switch
        {
            UrgencyLevel.High => 15,
            UrgencyLevel.Medium => 10,
            UrgencyLevel.Low => 5,
            _ => 10
        };
    }

    private static LoadViewRequestDto MapToDto(RoadRequest request, User? requester, bool includeResponses = false)
    {
        var dto = new LoadViewRequestDto
        {
            RequestId = request.RequestId,
            RequesterId = request.RequesterId,
            RequesterName = requester != null ? $"{requester.FirstName} {requester.LastName}".Trim() : null,
            Latitude = request.Location.Y,
            Longitude = request.Location.X,
            RadiusMeters = request.Radius,
            RequestType = request.RequestType,
            Description = request.Description,
            UrgencyLevel = request.UrgencyLevel,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt,
            ResponseCount = request.ResponseCount
        };

        if (includeResponses && request.Responses != null)
        {
            dto.Responses = request.Responses
                .Select(r => MapResponseToDto(r, r.Responder))
                .ToList();
        }

        return dto;
    }

    private static LoadViewResponseDto MapResponseToDto(RoadRequestResponse response, User? responder)
    {
        return new LoadViewResponseDto
        {
            ResponseId = response.ResponseId,
            RequestId = response.RequestId,
            ResponderId = response.ResponderId,
            ResponderName = responder != null ? $"{responder.FirstName} {responder.LastName}".Trim() : null,
            MediaUrl = response.MediaUrl,
            ThumbnailUrl = response.ThumbnailUrl,
            MediaType = response.MediaType,
            VideoDurationSeconds = response.VideoDurationSeconds,
            Caption = response.Caption,
            CreatedAt = response.CreatedAt,
            WasHelpful = response.WasHelpful,
            IsLiveStream = response.IsLiveStream,
            LiveKitRoomName = response.LiveKitRoomName,
            RewardPoints = response.RewardPoints
        };
    }
}
