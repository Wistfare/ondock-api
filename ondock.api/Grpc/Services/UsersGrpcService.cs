using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcUserProfileResponse = Ondock.Api.Grpc.V1.UserProfileResponse;
using GrpcUpdateProfileRequest = Ondock.Api.Grpc.V1.UpdateProfileRequest;
using GrpcUserSettingsResponse = Ondock.Api.Grpc.V1.UserSettingsResponse;
using GrpcUpdateSettingsRequest = Ondock.Api.Grpc.V1.UpdateSettingsRequest;
using GrpcGetNearbyUsersRequest = Ondock.Api.Grpc.V1.GetNearbyUsersRequest;
using GrpcNearbyUsersResponse = Ondock.Api.Grpc.V1.NearbyUsersResponse;
using GrpcDiscoveryUser = Ondock.Api.Grpc.V1.DiscoveryUser;
using GrpcUpdateLocationRequest = Ondock.Api.Grpc.V1.UpdateLocationRequest;
using GrpcStreamNearbyUsersRequest = Ondock.Api.Grpc.V1.StreamNearbyUsersRequest;
using GrpcNearbyUserEvent = Ondock.Api.Grpc.V1.NearbyUserEvent;
using GrpcNearbyUserEventType = Ondock.Api.Grpc.V1.NearbyUserEventType;

using DtoUpsertUserProfileRequest = ondock.api.DTOs.Chat.UpsertUserProfileRequest;
using DtoUpdateUserSettingsRequest = ondock.api.DTOs.Settings.UpdateUserSettingsRequest;

namespace ondock.api.Grpc.Services;

[Authorize]
public class UsersGrpcService : Ondock.Api.Grpc.V1.UsersService.UsersServiceBase
{
    private readonly IChatService _chatService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILocationService _locationService;
    private readonly ILogger<UsersGrpcService> _logger;

    // Track active nearby user streams for broadcasting location updates
    public static readonly ConcurrentDictionary<Guid, NearbyUserStreamContext> ActiveNearbyStreams = new();

    public UsersGrpcService(
        IChatService chatService,
        IUserSettingsService userSettingsService,
        ILocationService locationService,
        ILogger<UsersGrpcService> logger)
    {
        _chatService = chatService;
        _userSettingsService = userSettingsService;
        _locationService = locationService;
        _logger = logger;
    }

    // Context for each active nearby user stream
    public class NearbyUserStreamContext
    {
        public Channel<GrpcNearbyUserEvent> Channel { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusMiles { get; set; }
        public HashSet<string> KnownUserIds { get; set; } = new();
        public HashSet<string> ChatUserIds { get; set; } = new(); // Users with active chats to exclude
    }

    public override async Task<GrpcUserProfileResponse> GetProfile(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var profile = await _chatService.GetProfileAsync(userId);

        return new GrpcUserProfileResponse
        {
            HasProfile = profile.HasProfile,
            CategoryId = profile.Category?.CategoryId.ToString() ?? "",
            CategoryName = profile.Category?.Name ?? "",
            SubCategoryId = profile.SubCategory?.SubCategoryId.ToString() ?? "",
            SubCategoryName = profile.SubCategory?.Name ?? "",
            VehicleTypeId = profile.VehicleType?.VehicleTypeId.ToString() ?? "",
            VehicleTypeName = profile.VehicleType?.Name ?? "",
            VehicleBrandId = profile.VehicleBrand?.BrandId.ToString() ?? "",
            VehicleBrandName = profile.VehicleBrand?.Name ?? ""
        };
    }

    public override async Task<GrpcUserProfileResponse> UpdateProfile(GrpcUpdateProfileRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (string.IsNullOrEmpty(request.CategoryId) || !Guid.TryParse(request.CategoryId, out var categoryId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "CategoryId is required"));
        }

        var dto = new DtoUpsertUserProfileRequest
        {
            CategoryId = categoryId,
            SubCategoryId = string.IsNullOrEmpty(request.SubCategoryId) ? null : Guid.Parse(request.SubCategoryId),
            VehicleTypeId = string.IsNullOrEmpty(request.VehicleTypeId) ? null : Guid.Parse(request.VehicleTypeId),
            VehicleBrandId = string.IsNullOrEmpty(request.VehicleBrandId) ? null : Guid.Parse(request.VehicleBrandId)
        };

        var profile = await _chatService.UpsertProfileAsync(userId, dto);

        return new GrpcUserProfileResponse
        {
            HasProfile = profile.HasProfile,
            CategoryId = profile.Category?.CategoryId.ToString() ?? "",
            CategoryName = profile.Category?.Name ?? "",
            SubCategoryId = profile.SubCategory?.SubCategoryId.ToString() ?? "",
            SubCategoryName = profile.SubCategory?.Name ?? "",
            VehicleTypeId = profile.VehicleType?.VehicleTypeId.ToString() ?? "",
            VehicleTypeName = profile.VehicleType?.Name ?? "",
            VehicleBrandId = profile.VehicleBrand?.BrandId.ToString() ?? "",
            VehicleBrandName = profile.VehicleBrand?.Name ?? ""
        };
    }

    public override async Task<GrpcUserSettingsResponse> GetSettings(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var settings = await _userSettingsService.GetSettingsAsync(userId);

        var response = new GrpcUserSettingsResponse
        {
            DndEnabled = settings.DndEnabled,
            NotificationsEnabled = settings.NotifyChat,
            SoundEnabled = settings.NotificationSound,
            VibrationEnabled = settings.NotificationVibration,
            DiscoveryRadiusMiles = settings.DefaultChatRadiusMiles,
            ShowOnMap = settings.VisibleToNearby,
            AllowChatRequests = settings.AutoJoinNearbyChats
        };

        return response;
    }

    public override async Task<GrpcUserSettingsResponse> UpdateSettings(GrpcUpdateSettingsRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoUpdateUserSettingsRequest();

        if (request.DndEnabled != null)
            dto.DndEnabled = request.DndEnabled.Value;
        if (request.NotificationsEnabled != null)
            dto.NotifyChat = request.NotificationsEnabled.Value;
        if (request.SoundEnabled != null)
            dto.NotificationSound = request.SoundEnabled.Value;
        if (request.VibrationEnabled != null)
            dto.NotificationVibration = request.VibrationEnabled.Value;
        if (request.DiscoveryRadiusMiles != null)
            dto.DefaultChatRadiusMiles = request.DiscoveryRadiusMiles.Value;
        if (request.ShowOnMap != null)
            dto.VisibleToNearby = request.ShowOnMap.Value;
        if (request.AllowChatRequests != null)
            dto.AutoJoinNearbyChats = request.AllowChatRequests.Value;

        var settings = await _userSettingsService.UpdateSettingsAsync(userId, dto);

        var response = new GrpcUserSettingsResponse
        {
            DndEnabled = settings.DndEnabled,
            NotificationsEnabled = settings.NotifyChat,
            SoundEnabled = settings.NotificationSound,
            VibrationEnabled = settings.NotificationVibration,
            DiscoveryRadiusMiles = settings.DefaultChatRadiusMiles,
            ShowOnMap = settings.VisibleToNearby,
            AllowChatRequests = settings.AutoJoinNearbyChats
        };

        return response;
    }

    public override async Task<GrpcNearbyUsersResponse> GetNearbyUsers(GrpcGetNearbyUsersRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        double? radiusMiles = request.RadiusMiles;

        var result = await _locationService.GetNearbyUsersAsync(
            userId,
            request.Latitude,
            request.Longitude,
            request.SpeedMph,
            radiusMiles);

        var response = new GrpcNearbyUsersResponse();

        foreach (var user in result.Users)
        {
            var nearbyUser = new GrpcDiscoveryUser
            {
                AnonymousUserId = user.AnonymousUserId ?? "",
                DisplayName = user.DisplayName ?? "",
                DisplayColor = user.Color ?? "",
                Latitude = 0, // Not in DTO
                Longitude = 0, // Not in DTO
                DistanceMiles = user.DistanceMiles,
                CategoryName = "", // Not in DTO
                VehicleTypeName = "", // Not in DTO
                VehicleBrandName = "", // Not in DTO
                IsPermanentContact = false // Not in DTO
            };

            if (user.LastSeenAt.HasValue)
            {
                nearbyUser.LastSeen = Timestamp.FromDateTime(user.LastSeenAt.Value.ToUniversalTime());
            }

            response.Users.Add(nearbyUser);
        }

        return response;
    }

    public override async Task<GrpcEmpty> UpdateLocation(GrpcUpdateLocationRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        
        // Broadcast this user's location update to all nearby streams
        await BroadcastUserLocationUpdate(userId, request.Latitude, request.Longitude);
        
        return new GrpcEmpty();
    }

    // === STREAMING RPC - Real-time nearby user updates ===
    public override async Task StreamNearbyUsers(GrpcStreamNearbyUsersRequest request, IServerStreamWriter<GrpcNearbyUserEvent> responseStream, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var radiusMiles = request.RadiusMiles ?? 10.0;
        
        _logger.LogInformation("gRPC: User {UserId} started streaming nearby users at ({Lat}, {Lng}), radius={Radius}mi",
            userId, request.Latitude, request.Longitude, radiusMiles);

        // Create channel for this user's events
        var channel = Channel.CreateUnbounded<GrpcNearbyUserEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var streamContext = new NearbyUserStreamContext
        {
            Channel = channel,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMiles = radiusMiles
        };

        ActiveNearbyStreams[userId] = streamContext;

        try
        {
            // Send initial batch of nearby users
            _logger.LogInformation("gRPC: Fetching nearby users for {UserId} at ({Lat}, {Lng})", 
                userId, request.Latitude, request.Longitude);
            
            var initialUsers = await _locationService.GetNearbyUsersAsync(
                userId, request.Latitude, request.Longitude, 0, radiusMiles);

            // Get active chat participant IDs to filter out users with existing chats
            var activeChats = await _chatService.GetActiveChatsAsync(userId, request.Latitude, request.Longitude);
            var chatUserIds = activeChats.Chats
                .Select(c => c.OtherAnonymousUserId ?? "")
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            // Filter out users who already have active chats
            var filteredUsers = initialUsers.Users
                .Where(u => !chatUserIds.Contains(u.AnonymousUserId ?? ""))
                .ToList();

            _logger.LogInformation("gRPC: LocationService returned {Count} nearby users for {UserId}, {Filtered} after filtering active chats", 
                initialUsers.Users.Count, userId, filteredUsers.Count);

            var initialEvent = new GrpcNearbyUserEvent
            {
                EventType = GrpcNearbyUserEventType.Initial,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            foreach (var user in filteredUsers)
            {
                _logger.LogDebug("gRPC: Adding user {AnonymousId} at distance {Distance}mi to response",
                    user.AnonymousUserId, user.DistanceMiles);
                var grpcUser = MapToGrpcDiscoveryUser(user);
                initialEvent.Users.Add(grpcUser);
                streamContext.KnownUserIds.Add(user.AnonymousUserId ?? "");
            }
            
            // Store chat user IDs in context for refresh filtering
            streamContext.ChatUserIds = chatUserIds;

            await responseStream.WriteAsync(initialEvent);
            _logger.LogInformation("gRPC: Sent initial {Count} nearby users to {UserId}", initialUsers.Users.Count, userId);

            // Keep stream alive with periodic updates
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            
            while (!context.CancellationToken.IsCancellationRequested)
            {
                // Check for events from channel (location updates from other users)
                while (channel.Reader.TryRead(out var eventFromChannel))
                {
                    await responseStream.WriteAsync(eventFromChannel);
                }

                // Periodic refresh to catch any changes
                if (await timer.WaitForNextTickAsync(context.CancellationToken))
                {
                    await RefreshNearbyUsers(userId, streamContext, responseStream);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("gRPC: User {UserId} disconnected from nearby users stream", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error in nearby users stream for {UserId}", userId);
        }
        finally
        {
            ActiveNearbyStreams.TryRemove(userId, out _);
            channel.Writer.Complete();
        }
    }

    private async Task RefreshNearbyUsers(Guid userId, NearbyUserStreamContext ctx, IServerStreamWriter<GrpcNearbyUserEvent> responseStream)
    {
        try
        {
            var currentUsers = await _locationService.GetNearbyUsersAsync(
                userId, ctx.Latitude, ctx.Longitude, 0, ctx.RadiusMiles);

            // Refresh active chat user IDs to filter out users with existing chats
            var activeChats = await _chatService.GetActiveChatsAsync(userId, ctx.Latitude, ctx.Longitude);
            ctx.ChatUserIds = activeChats.Chats
                .Select(c => c.OtherAnonymousUserId ?? "")
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            // Filter out users who have active chats
            var filteredUsers = currentUsers.Users
                .Where(u => !ctx.ChatUserIds.Contains(u.AnonymousUserId ?? ""))
                .ToList();

            var currentUserIds = filteredUsers
                .Select(u => u.AnonymousUserId ?? "")
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            // Detect users who left (or started a chat)
            var leftUserIds = ctx.KnownUserIds.Except(currentUserIds).ToList();
            if (leftUserIds.Any())
            {
                var leftEvent = new GrpcNearbyUserEvent
                {
                    EventType = GrpcNearbyUserEventType.UserLeft,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                foreach (var leftId in leftUserIds)
                {
                    leftEvent.Users.Add(new GrpcDiscoveryUser { AnonymousUserId = leftId });
                }
                await responseStream.WriteAsync(leftEvent);
            }

            // Detect new users who joined (and don't have active chats)
            var joinedUserIds = currentUserIds.Except(ctx.KnownUserIds).ToList();
            if (joinedUserIds.Any())
            {
                var joinedEvent = new GrpcNearbyUserEvent
                {
                    EventType = GrpcNearbyUserEventType.UserJoined,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                foreach (var user in filteredUsers.Where(u => joinedUserIds.Contains(u.AnonymousUserId ?? "")))
                {
                    joinedEvent.Users.Add(MapToGrpcDiscoveryUser(user));
                }
                await responseStream.WriteAsync(joinedEvent);
            }

            // Update known users
            ctx.KnownUserIds = currentUserIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing nearby users for stream");
        }
    }

    private async Task BroadcastUserLocationUpdate(Guid movingUserId, double latitude, double longitude)
    {
        // Get the anonymous ID for this user
        var profile = await _chatService.GetProfileAsync(movingUserId);
        var anonymousId = "anon_" + movingUserId.ToString("N").Substring(0, 8);

        foreach (var (streamUserId, ctx) in ActiveNearbyStreams)
        {
            if (streamUserId == movingUserId) continue; // Don't notify self

            // Check if this user is within range of the stream subscriber
            var distanceMiles = CalculateDistanceMiles(ctx.Latitude, ctx.Longitude, latitude, longitude);
            
            if (distanceMiles <= ctx.RadiusMiles)
            {
                var moveEvent = new GrpcNearbyUserEvent
                {
                    EventType = GrpcNearbyUserEventType.UserMoved,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                moveEvent.Users.Add(new GrpcDiscoveryUser
                {
                    AnonymousUserId = anonymousId,
                    Latitude = latitude,
                    Longitude = longitude,
                    DistanceMiles = distanceMiles
                });

                try
                {
                    await ctx.Channel.Writer.WriteAsync(moveEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write location update to stream for user {UserId}", streamUserId);
                }
            }
        }
    }

    private static double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMiles = 3958.8;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMiles * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static GrpcDiscoveryUser MapToGrpcDiscoveryUser(ondock.api.DTOs.Chat.NearbyUserDto user)
    {
        var grpcUser = new GrpcDiscoveryUser
        {
            AnonymousUserId = user.AnonymousUserId ?? "",
            DisplayName = user.DisplayName ?? "",
            DisplayColor = user.Color ?? "",
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            DistanceMiles = user.DistanceMiles,
            IsPermanentContact = false
        };
        if (user.LastSeenAt.HasValue)
        {
            grpcUser.LastSeen = Timestamp.FromDateTime(user.LastSeenAt.Value.ToUniversalTime());
        }
        return grpcUser;
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
