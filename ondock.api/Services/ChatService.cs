using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Text.Json;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Exceptions;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class ChatService : IChatService
{
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly OnDockDbContext _db;
    private readonly IChatSettingsProvider _settingsProvider;
    private readonly IChatRealtimeService _realtime;
    private readonly ILogger<ChatService> _logger;
    private readonly IMagicPushService _magicPush;
    private readonly IFcmNotificationService _fcmNotificationService;

    public ChatService(
        OnDockDbContext db,
        IChatSettingsProvider settingsProvider,
        IChatRealtimeService realtime,
        ILogger<ChatService> logger,
        IMagicPushService magicPush,
        IFcmNotificationService fcmNotificationService)
    {
        _db = db;
        _settingsProvider = settingsProvider;
        _realtime = realtime;
        _logger = logger;
        _magicPush = magicPush;
        _fcmNotificationService = fcmNotificationService;
    }

    public async Task<NearbyUsersResponse> GetNearbyUsersAsync(Guid userId, double latitude, double longitude, double speedMph, double? radiusMiles)
    {
        var settings = _settingsProvider.Get();
        var effectiveRadiusMiles = ClampRadiusMiles(radiusMiles, settings);
        var bypassRestrictions = settings.BypassNearbyRestrictions;

        await UpsertUserLocationAsync(userId, latitude, longitude, speedMph);

        if (!bypassRestrictions && speedMph > settings.SpeedThresholdMph)
        {
            return new NearbyUsersResponse { Count = 0 };
        }

        var maxMeters = MilesToMeters(effectiveRadiusMiles);
        var presenceCutoff = DateTime.UtcNow.AddSeconds(-settings.PresenceMaxAgeSeconds);

        var nearbyQuery = _db.UserLocations.Where(l => l.UserId != userId);
        if (!bypassRestrictions)
        {
            nearbyQuery = nearbyQuery.Where(l => l.Timestamp >= presenceCutoff);
        }
        var nearby = await nearbyQuery
            .Select(l => new { l.UserId, l.CurrentLocation, l.Speed, l.Timestamp })
            .ToListAsync();

        var withinQuery = nearby
            .Select(x => new
            {
                x.UserId,
                DistanceMeters = HaversineMeters(latitude, longitude, x.CurrentLocation.Y, x.CurrentLocation.X),
                Speed = (double)x.Speed
            });

        if (!bypassRestrictions)
        {
            withinQuery = withinQuery.Where(x => x.DistanceMeters <= maxMeters && x.Speed <= settings.SpeedThresholdMph);
        }

        var within = withinQuery
            .OrderBy(x => x.DistanceMeters)
            .Take(100)
            .ToList();

        if (!within.Any())
        {
            return new NearbyUsersResponse { Count = 0 };
        }

        var otherUserIdsInitial = within.Select(x => x.UserId).ToList();
        var dndUserIds = await GetChatDndUserIdsAsync(otherUserIdsInitial);
        within = within.Where(x => !dndUserIds.Contains(x.UserId)).ToList();

        if (!within.Any())
        {
            return new NearbyUsersResponse { Count = 0 };
        }

        var otherUserIds = within.Select(x => x.UserId).ToList();

        // Get shared chats
        var myChats = await _db.ChatParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ChatId)
            .ToListAsync();

        var otherParticipants = await _db.ChatParticipants
            .Where(p => myChats.Contains(p.ChatId) && otherUserIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.ChatId })
            .ToListAsync();

        var hasChatByUser = otherParticipants
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().ChatId);

        // Fetch last messages
        var activeChatIds = hasChatByUser.Values.Distinct().ToList();
        var lastMessagesByChat = new Dictionary<string, (string? Content, DateTime Timestamp)>();

        if (activeChatIds.Count > 0)
        {
            var latestMessages = await _db.Messages
                .AsNoTracking()
                .Where(m => activeChatIds.Contains(m.ChatId))
                .GroupBy(m => m.ChatId)
                .Select(g => g.OrderByDescending(m => m.Timestamp).FirstOrDefault())
                .ToListAsync();

            foreach (var msg in latestMessages)
            {
                if (msg != null)
                {
                    lastMessagesByChat[msg.ChatId] = (msg.EncryptedContent, msg.Timestamp);
                }
            }
        }

        // Get profiles for interest matching
        var myProfile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        _logger.LogInformation("[NearbyUsers] User {UserId} profile: {HasProfile}, CategoryId: {CategoryId}, SubCategoryId: {SubCategoryId}, VehicleTypeId: {VehicleTypeId}, VehicleBrandId: {VehicleBrandId}",
            userId,
            myProfile != null,
            myProfile?.CategoryId,
            myProfile?.SubCategoryId,
            myProfile?.VehicleTypeId,
            myProfile?.VehicleBrandId);

        var otherProfiles = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p);

        _logger.LogInformation("[NearbyUsers] Found {OtherProfileCount} profiles for {OtherUserCount} nearby users",
            otherProfiles.Count, otherUserIds.Count);

        // Get posts for nearby users (grouped by user)
        var recentPostCutoff = DateTime.UtcNow.AddHours(-24);
        var userPosts = await _db.Posts
            .Where(p => otherUserIds.Contains(p.UserId) && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var postsByUser = userPosts
            .GroupBy(p => p.UserId)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(p => new DTOs.Post.PostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    UserDisplayName = p.AnonymousIdentifier,
                    Latitude = p.Location.Y,
                    Longitude = p.Location.X,
                    RoadIdentification = p.RoadIdentification,
                    PostType = p.PostType,
                    MediaUrl = p.MediaUrl,
                    Description = p.Description,
                    UrgencyLevel = p.UrgencyLevel,
                    CreatedAt = p.CreatedAt,
                    ExpiresAt = p.ExpiresAt,
                    ViewCount = p.ViewCount,
                    VideoDurationSeconds = p.VideoDurationSeconds,
                    HasAudio = p.HasAudio
                }).ToList()
            );

        var resultUsers = new List<NearbyUserDto>();
        foreach (var u in within)
        {
            var hasChat = hasChatByUser.TryGetValue(u.UserId, out var chatId);
            string? lastMessage = null;
            DateTime? lastMessageTimestamp = null;

            if (hasChat && lastMessagesByChat.TryGetValue(chatId, out var msgInfo))
            {
                lastMessage = msgInfo.Content;
                lastMessageTimestamp = msgInfo.Timestamp;
            }

            // Calculate common interests
            var commonInterests = new List<string>();
            if (myProfile != null && otherProfiles.TryGetValue(u.UserId, out var otherProfile))
            {
                commonInterests = GetCommonInterests(myProfile, otherProfile);
                _logger.LogDebug("[NearbyUsers] Comparing profiles - My: Cat={MyCat}, Sub={MySub}, Type={MyType}, Brand={MyBrand} vs Other: Cat={OtherCat}, Sub={OtherSub}, Type={OtherType}, Brand={OtherBrand} => CommonCount={Count}",
                    myProfile.CategoryId, myProfile.SubCategoryId, myProfile.VehicleTypeId, myProfile.VehicleBrandId,
                    otherProfile.CategoryId, otherProfile.SubCategoryId, otherProfile.VehicleTypeId, otherProfile.VehicleBrandId,
                    commonInterests.Count);
            }
            else
            {
                _logger.LogDebug("[NearbyUsers] Cannot compare - MyProfile={HasMyProfile}, OtherProfile={HasOtherProfile} for user {OtherUserId}",
                    myProfile != null, otherProfiles.ContainsKey(u.UserId), u.UserId);
            }

            // Get posts for this user
            var userPostsList = postsByUser.TryGetValue(u.UserId, out var posts) ? posts : new List<DTOs.Post.PostDto>();
            var hasRecentPost = userPostsList.Any(p => p.CreatedAt >= recentPostCutoff);

            resultUsers.Add(new NearbyUserDto
            {
                AnonymousUserId = MakeAnonymousUserId(u.UserId),
                DisplayName = MakeAnonymousDisplayName(u.UserId),
                Color = hasChat ? PickColorForUser(u.UserId, settings) : settings.NearbyUserColor,
                DistanceMiles = MetersToMiles(u.DistanceMeters),
                HasActiveChat = hasChat,
                ChatId = hasChat ? chatId : null,
                CommonInterestsCount = commonInterests.Count,
                CommonInterests = commonInterests,
                LastMessage = lastMessage,
                LastMessageTimestamp = lastMessageTimestamp,
                PostCount = userPostsList.Count,
                HasRecentPost = hasRecentPost,
                Posts = userPostsList
            });
        }

        return new NearbyUsersResponse
        {
            Count = resultUsers.Count,
            Users = resultUsers
        };
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return new UserProfileResponse { HasProfile = false };
        }

        return new UserProfileResponse
        {
            HasProfile = true,
            Category = profile.Category != null ? new ProfileCategoryDto
            {
                CategoryId = profile.Category.CategoryId,
                Name = profile.Category.Name,
                SortOrder = profile.Category.SortOrder
            } : null,
            SubCategory = profile.SubCategory != null ? new ProfileSubCategoryDto
            {
                SubCategoryId = profile.SubCategory.SubCategoryId,
                CategoryId = profile.SubCategory.CategoryId,
                Name = profile.SubCategory.Name,
                SortOrder = profile.SubCategory.SortOrder
            } : null,
            VehicleType = profile.VehicleType != null ? new VehicleTypeDto
            {
                VehicleTypeId = profile.VehicleType.VehicleTypeId,
                SubCategoryId = profile.VehicleType.SubCategoryId,
                Name = profile.VehicleType.Name,
                SortOrder = profile.VehicleType.SortOrder
            } : null,
            VehicleBrand = profile.VehicleBrand != null ? new VehicleBrandDto
            {
                BrandId = profile.VehicleBrand.BrandId,
                VehicleTypeId = profile.VehicleBrand.VehicleTypeId,
                Name = profile.VehicleBrand.Name,
                SortOrder = profile.VehicleBrand.SortOrder
            } : null
        };
    }

    public async Task<UserProfileResponse> UpsertProfileAsync(Guid userId, UpsertUserProfileRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request is required");
        }

        // Validate category exists
        var categoryExists = await _db.ProfileCategories.AnyAsync(c => c.CategoryId == request.CategoryId && c.IsActive);
        if (!categoryExists)
        {
            throw new BadRequestException("Invalid CategoryId");
        }

        var existing = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing == null)
        {
            _db.UserProfiles.Add(new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                CategoryId = request.CategoryId,
                SubCategoryId = request.SubCategoryId,
                VehicleTypeId = request.VehicleTypeId,
                VehicleBrandId = request.VehicleBrandId,
                CustomCategory = request.CustomCategory,
                CustomSubCategory = request.CustomSubCategory,
                CustomVehicleType = request.CustomVehicleType,
                CustomVehicleBrand = request.CustomBrand,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.CategoryId = request.CategoryId;
            existing.SubCategoryId = request.SubCategoryId;
            existing.VehicleTypeId = request.VehicleTypeId;
            existing.VehicleBrandId = request.VehicleBrandId;
            existing.CustomCategory = request.CustomCategory;
            existing.CustomSubCategory = request.CustomSubCategory;
            existing.CustomVehicleType = request.CustomVehicleType;
            existing.CustomVehicleBrand = request.CustomBrand;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return await GetProfileAsync(userId);
    }

    public async Task<CreateAnnouncementResponse> CreateAnnouncementAsync(Guid userId, CreateAnnouncementRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request is required");
        }

        var settings = _settingsProvider.Get();
        var effectiveRadiusMiles = ClampRadiusMiles(null, settings);

        await UpsertUserLocationAsync(userId, request.Latitude, request.Longitude, request.SpeedMph);

        if (request.SpeedMph > settings.SpeedThresholdMph)
        {
            throw new BadRequestException($"Cannot post announcement while travelling faster than {settings.SpeedThresholdMph:0.#} MPH");
        }

        var announcementId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Create proper Announcement entity
        var announcement = new Announcement
        {
            AnnouncementId = announcementId,
            AuthorId = userId,
            Title = request.Title,
            Body = request.Body,
            UrgencyLevel = request.UrgencyLevel,
            OriginLocation = MakePoint(request.Longitude, request.Latitude),
            RadiusMiles = effectiveRadiusMiles,
            CreatedAt = now,
            ExpiresAt = now.AddHours(24),
            IsActive = true
        };
        _db.Announcements.Add(announcement);

        // Find nearby users for notification
        var maxMeters = MilesToMeters(effectiveRadiusMiles);
        var presenceCutoff = DateTime.UtcNow.AddSeconds(-settings.PresenceMaxAgeSeconds);

        var nearby = await _db.UserLocations
            .Where(l => l.UserId != userId && l.Timestamp >= presenceCutoff)
            .Select(l => new { l.UserId, l.CurrentLocation, l.Speed })
            .ToListAsync();

        var candidates = nearby
            .Select(x => new
            {
                x.UserId,
                DistanceMeters = HaversineMeters(request.Latitude, request.Longitude, x.CurrentLocation.Y, x.CurrentLocation.X),
                Speed = (double)x.Speed
            })
            .Where(x => x.DistanceMeters <= maxMeters && x.Speed <= settings.SpeedThresholdMph)
            .OrderBy(x => x.DistanceMeters)
            .Take(200)
            .ToList();

        var dndUserIds = await GetChatDndUserIdsAsync(candidates.Select(x => x.UserId).ToList());
        var recipients = candidates.Select(x => x.UserId).Where(id => !dndUserIds.Contains(id)).Distinct().ToList();

        await _db.SaveChangesAsync();

        return new CreateAnnouncementResponse
        {
            AnnouncementId = announcementId.ToString(),
            RecipientCount = recipients.Count,
            Timestamp = now
        };
    }

    public async Task<ChatInboxResponse> GetInboxAsync(Guid userId, double? latitude, double? longitude, int take)
    {
        var settings = _settingsProvider.Get();
        var effectiveRadiusMiles = ClampRadiusMiles(null, settings);
        var maxMeters = MilesToMeters(effectiveRadiusMiles);

        // Get user's profile for anonymous ID generation
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        // Query active announcements
        var query = _db.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(a => a.CreatedAt)
            .Take(take);

        var announcements = await query
            .Select(a => new
            {
                a.AnnouncementId,
                a.AuthorId,
                a.Title,
                a.Body,
                a.UrgencyLevel,
                a.OriginLocation,
                a.CreatedAt,
                a.ExpiresAt,
                a.ViewCount,
                a.ReplyCount
            })
            .ToListAsync();

        var items = new List<ChatInboxItemDto>();
        foreach (var a in announcements)
        {
            var displayName = MakeAnonymousDisplayName(a.AuthorId);
            var colorName = PickColorForUser(a.AuthorId, settings);

            // Calculate distance if user location is provided
            double? distanceMiles = null;
            if (latitude.HasValue && longitude.HasValue)
            {
                var distanceMeters = HaversineMeters(latitude.Value, longitude.Value, a.OriginLocation.Y, a.OriginLocation.X);
                distanceMiles = MetersToMiles(distanceMeters);
            }

            var isMine = a.AuthorId == userId;

            items.Add(new ChatInboxItemDto
            {
                Id = a.AnnouncementId.ToString(),
                Kind = "chat_announcement",
                Title = a.Title,
                Body = a.Body,
                Timestamp = a.CreatedAt,
                AnnouncementId = a.AnnouncementId.ToString(),
                FromAnonymousUserId = $"anon_{a.AuthorId}",
                FromDisplayName = displayName,
                FromColor = colorName,
                UrgencyLevel = (int)a.UrgencyLevel,
                DistanceMiles = distanceMiles,
                ExpiresAt = a.ExpiresAt,
                ViewCount = a.ViewCount,
                ReplyCount = a.ReplyCount,
                IsMine = isMine
            });
        }

        return new ChatInboxResponse
        {
            Count = items.Count,
            Items = items
        };
    }

    public async Task<ReplyToAnnouncementResponse> ReplyToAnnouncementAsync(Guid userId, ReplyToAnnouncementRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request is required");
        }

        if (!TryParseAnonymousUserId(request.TargetAnonymousUserId, out var targetUserId))
        {
            throw new BadRequestException("Invalid TargetAnonymousUserId");
        }

        // Prevent replying to own announcement
        if (userId == targetUserId)
        {
            throw new BadRequestException("Cannot reply to your own announcement");
        }

        var settings = _settingsProvider.Get();
        var now = DateTime.UtcNow;

        Guid? parsedAnnouncementId = null;
        if (Guid.TryParse(request.AnnouncementId, out var announcementId))
        {
            parsedAnnouncementId = announcementId;
        }

        // Create or get existing chat, linking to the announcement
        var chatId = await GetOrCreateChatAsync(userId, targetUserId, settings, null, settings.DefaultRadiusMiles, parsedAnnouncementId);
        var expiresAt = now.AddMinutes(settings.RoomExpiryMinutes);

        await _db.SaveChangesAsync();

        return new ReplyToAnnouncementResponse
        {
            ChatId = chatId,
            ExpiresAt = expiresAt
        };
    }

    public async Task<ReportChatResponse> ReportChatAsync(Guid userId, ReportChatRequest request)
    {
        var reportId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _db.NotificationLogs.Add(new NotificationLog
        {
            NotificationId = reportId,
            UserId = userId,
            Type = NotificationType.SystemAnnouncement,
            Title = "Chat Report",
            Body = $"Report: {request.Reason}",
            Data = JsonSerializer.Serialize(new { request.ChatId, request.Reason }),
            SentAt = now,
            DeviceCount = 0,
            DeliveryStatus = DeliveryStatus.Sent
        });

        await _db.SaveChangesAsync();

        return new ReportChatResponse
        {
            ReportId = reportId.ToString(),
            Timestamp = now
        };
    }

    public async Task<JoinChatResponse> JoinAsync(Guid userId, JoinChatRequest request)
    {
        var settings = _settingsProvider.Get();
        var effectiveRadiusMiles = ClampRadiusMiles(request.RadiusMiles, settings);

        if (request.SpeedMph > settings.SpeedThresholdMph)
        {
            throw new BadRequestException($"Cannot start chat while travelling faster than {settings.SpeedThresholdMph:0.#} MPH");
        }

        if (string.IsNullOrWhiteSpace(request.TargetAnonymousUserId))
        {
            throw new BadRequestException("TargetAnonymousUserId is required");
        }

        if (!TryParseAnonymousUserId(request.TargetAnonymousUserId, out var targetUserId))
        {
            throw new BadRequestException("Invalid TargetAnonymousUserId");
        }

        if (targetUserId == userId)
        {
            throw new BadRequestException("Cannot chat with yourself");
        }

        await UpsertUserLocationAsync(userId, request.Latitude, request.Longitude, request.SpeedMph);

        var center = MakePoint(request.Longitude, request.Latitude);
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.RoomExpiryMinutes);

        // Parse announcement ID if provided
        Guid? announcementId = null;
        if (!string.IsNullOrEmpty(request.AnnouncementId) && Guid.TryParse(request.AnnouncementId, out var parsedAnnId))
        {
            announcementId = parsedAnnId;
        }

        // Get or create chat
        var chatId = await GetOrCreateChatAsync(userId, targetUserId, settings, center, effectiveRadiusMiles, announcementId);
        var chat = await _db.Chats.FirstAsync(c => c.ChatId == chatId);

        // Ensure participants
        await EnsureParticipantAsync(chatId, userId, request.Latitude, request.Longitude, request.SpeedMph, request.PublicKey, settings);

        var targetLocation = await _db.UserLocations.FirstOrDefaultAsync(l => l.UserId == targetUserId);
        var targetLat = targetLocation?.CurrentLocation.Y ?? request.Latitude;
        var targetLon = targetLocation?.CurrentLocation.X ?? request.Longitude;
        var targetSpeed = targetLocation?.Speed ?? 0;

        await EnsureParticipantAsync(chatId, targetUserId, targetLat, targetLon, targetSpeed, null, settings);

        await _db.SaveChangesAsync();

        chat.ActiveUserCount = await _db.ChatParticipants.CountAsync(p => p.ChatId == chatId);
        await _db.SaveChangesAsync();

        return new JoinChatResponse
        {
            ChatId = chatId,
            SelfTemporaryUserId = MakeTemporaryUserId(userId, chatId),
            SelfDisplayName = MakeAnonymousDisplayName(userId),
            SelfDisplayColor = PickColorForUser(userId, settings),
            OtherTemporaryUserId = MakeTemporaryUserId(targetUserId, chatId),
            OtherDisplayName = MakeAnonymousDisplayName(targetUserId),
            OtherDisplayColor = PickColorForUser(targetUserId, settings),
            OtherUserId = targetUserId,
            ExpiresAt = expiresAt
        };
    }

    public async Task LeaveAsync(Guid userId, LeaveChatRequest request)
    {
        var chatId = request.ChatId;

        var settings = _settingsProvider.Get();
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId);
        if (chat == null)
        {
            return;
        }

        var participant = await _db.ChatParticipants.FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (participant == null)
        {
            return;
        }

        _db.ChatParticipants.Remove(participant);
        await _db.SaveChangesAsync();

        chat.ActiveUserCount = await _db.ChatParticipants.CountAsync(p => p.ChatId == chatId);
        await _db.SaveChangesAsync();

        if (settings.DeleteRoomOnAnyExit)
        {
            await EndChatAsync(chatId, reason: "participant_left");
        }
    }

    public async Task<ChatMessageResponse> SendMessageAsync(Guid userId, SendMessageRequest request)
    {
        var settings = _settingsProvider.Get();

        if (request.Latitude.HasValue && request.Longitude.HasValue && request.SpeedMph.HasValue)
        {
            await UpsertUserLocationAsync(userId, request.Latitude.Value, request.Longitude.Value, request.SpeedMph.Value);
        }

        var sender = await _db.ChatParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChatId == request.ChatId && p.UserId == userId);

        if (sender == null)
        {
            throw new ForbiddenException("You are not a participant of this chat");
        }

        var msg = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatId = request.ChatId,
            SenderId = sender.ParticipantId,
            EncryptedContent = request.EncryptedContent,
            MessageType = request.MessageType,
            AnnouncementId = request.AnnouncementId,
            QuotedContent = request.QuotedContent,
            QuotedTitle = request.QuotedTitle,
            Timestamp = DateTime.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        var payload = new ChatMessageResponse
        {
            MessageId = msg.MessageId,
            ChatId = msg.ChatId,
            SenderUserId = userId,
            SenderTemporaryUserId = MakeTemporaryUserId(userId, msg.ChatId),
            SenderDisplayName = MakeAnonymousDisplayName(userId),
            SenderDisplayColor = sender.DisplayColor,
            EncryptedContent = msg.EncryptedContent,
            MessageType = msg.MessageType,
            AnnouncementId = msg.AnnouncementId,
            QuotedContent = msg.QuotedContent,
            QuotedTitle = msg.QuotedTitle,
            Timestamp = msg.Timestamp
        };

        await _realtime.SendMessageReceivedAsync(request.ChatId, payload);

        try
        {
            var recipientUserIds = await _db.ChatParticipants
                .AsNoTracking()
                .Where(p => p.ChatId == request.ChatId && p.UserId != userId)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            var title = payload.SenderDisplayName;
            var body = payload.MessageType switch
            {
                MessageType.Text when payload.EncryptedContent?.StartsWith("[LOADVIEW_REQUEST:") == true => "Road View Request",
                MessageType.Text when payload.EncryptedContent?.StartsWith("[LOADVIEW_RESPONSE:") == true => "Road View Response",
                MessageType.Text when payload.EncryptedContent?.StartsWith("[LOADVIEW_DECLINED:") == true => "Road View Declined",
                MessageType.Text => "New message",
                MessageType.Image => "Photo",
                MessageType.Audio => "Voice message",
                MessageType.Video => "Video",
                MessageType.File => "File",
                _ => "New message"
            };

            await _fcmNotificationService.SendChatMessageAsync(
                recipientUserIds,
                payload.ChatId,
                payload.MessageId,
                title,
                body,
                payload.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send chat push notification for chat {ChatId} message {MessageId}", payload.ChatId, payload.MessageId);
        }

        return payload;
    }

    public async Task<ChatMessageResponse> SendMediaMessageAsync(Guid userId, SendMessageRequest request, string mediaUrl, string fileName, long fileSize, int? durationSeconds)
    {
        var settings = _settingsProvider.Get();

        if (request.Latitude.HasValue && request.Longitude.HasValue && request.SpeedMph.HasValue)
        {
            await UpsertUserLocationAsync(userId, request.Latitude.Value, request.Longitude.Value, request.SpeedMph.Value);
        }

        var sender = await _db.ChatParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChatId == request.ChatId && p.UserId == userId);

        if (sender == null)
        {
            throw new ForbiddenException("You are not a participant of this chat");
        }

        var msg = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatId = request.ChatId,
            SenderId = sender.ParticipantId,
            EncryptedContent = request.EncryptedContent,
            MessageType = request.MessageType,
            MediaUrl = mediaUrl,
            MediaFileName = fileName,
            MediaFileSize = fileSize,
            MediaDurationSeconds = durationSeconds,
            Timestamp = DateTime.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        var payload = new ChatMessageResponse
        {
            MessageId = msg.MessageId,
            ChatId = msg.ChatId,
            SenderUserId = userId,
            SenderTemporaryUserId = MakeTemporaryUserId(userId, msg.ChatId),
            SenderDisplayName = MakeAnonymousDisplayName(userId),
            SenderDisplayColor = sender.DisplayColor,
            EncryptedContent = msg.EncryptedContent,
            MessageType = msg.MessageType,
            MediaUrl = msg.MediaUrl,
            MediaFileName = msg.MediaFileName,
            MediaFileSize = msg.MediaFileSize,
            MediaDurationSeconds = msg.MediaDurationSeconds,
            Timestamp = msg.Timestamp
        };

        await _realtime.SendMessageReceivedAsync(request.ChatId, payload);

        // Send push notification for media messages
        try
        {
            var recipientUserIds = await _db.ChatParticipants
                .AsNoTracking()
                .Where(p => p.ChatId == request.ChatId && p.UserId != userId)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            var title = payload.SenderDisplayName;
            // Check if this is a LoadView response (caption contains the format)
            var isLoadViewResponse = request.EncryptedContent?.StartsWith("[LOADVIEW_RESPONSE:") == true;
            var body = isLoadViewResponse ? "Road View Response" : request.MessageType switch
            {
                MessageType.Image => "Photo",
                MessageType.Audio => "Voice message",
                MessageType.Video => "Video",
                MessageType.File => "File",
                _ => "Media"
            };

            await _fcmNotificationService.SendChatMessageAsync(
                recipientUserIds,
                payload.ChatId,
                payload.MessageId,
                title,
                body,
                payload.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send media push notification for chat {ChatId} message {MessageId}", payload.ChatId, payload.MessageId);
        }

        return payload;
    }

    public async Task<ChatHistoryResponse> GetHistoryAsync(Guid userId, string chatId, int take)
    {
        if (take <= 0) take = 25;
        take = Math.Min(take, 200);

        var isParticipant = await _db.ChatParticipants.AnyAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (!isParticipant)
        {
            throw new ForbiddenException("You are not a participant of this chat");
        }

        var messages = await _db.Messages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.Timestamp)
            .Take(take)
            .Join(
                _db.ChatParticipants.AsNoTracking(),
                m => m.SenderId,
                p => p.ParticipantId,
                (m, p) => new { m, p })
            .ToListAsync();

        var response = new ChatHistoryResponse
        {
            SelfTemporaryUserId = MakeTemporaryUserId(userId, chatId),
            Messages = messages
                .Select(x => new ChatMessageResponse
                {
                    MessageId = x.m.MessageId,
                    ChatId = chatId,
                    SenderUserId = x.p.UserId,
                    SenderTemporaryUserId = MakeTemporaryUserId(x.p.UserId, chatId),
                    SenderDisplayName = MakeAnonymousDisplayName(x.p.UserId),
                    SenderDisplayColor = x.p.DisplayColor,
                    EncryptedContent = x.m.EncryptedContent,
                    MessageType = x.m.MessageType,
                    MediaUrl = x.m.MediaUrl,
                    MediaFileName = x.m.MediaFileName,
                    MediaFileSize = x.m.MediaFileSize,
                    MediaDurationSeconds = x.m.MediaDurationSeconds,
                    AnnouncementId = x.m.AnnouncementId,
                    QuotedContent = x.m.QuotedContent,
                    QuotedTitle = x.m.QuotedTitle,
                    Timestamp = x.m.Timestamp,
                    ReadAt = x.m.ReadAt
                })
                .OrderByDescending(m => m.Timestamp)
                .ToList()
        };

        return response;
    }

    public async Task<ActiveChatsResponse> GetActiveChatsAsync(Guid userId, double? latitude = null, double? longitude = null)
    {
        var settings = _settingsProvider.Get();
        var now = DateTime.UtcNow;

        var myChats = await _db.ChatParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.ChatId)
            .ToListAsync();

        if (myChats.Count == 0)
        {
            return new ActiveChatsResponse { Count = 0, Chats = new List<ActiveChatDto>() };
        }

        var chats = await _db.Chats
            .AsNoTracking()
            .Include(c => c.Announcement)
            .Where(c => myChats.Contains(c.ChatId))
            .ToDictionaryAsync(c => c.ChatId, c => c);

        var activeChatIds = chats.Keys.ToList();
        if (activeChatIds.Count == 0)
        {
            return new ActiveChatsResponse { Count = 0, Chats = new List<ActiveChatDto>() };
        }

        var otherParticipants = await _db.ChatParticipants
            .AsNoTracking()
            .Where(p => activeChatIds.Contains(p.ChatId) && p.UserId != userId)
            .ToListAsync();

        var otherUserIds = otherParticipants.Select(p => p.UserId).Distinct().ToList();

        var myProfile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        _logger.LogInformation("[ActiveChats] User {UserId} profile: {HasProfile}, CategoryId: {CategoryId}, SubCategoryId: {SubCategoryId}, VehicleTypeId: {VehicleTypeId}, VehicleBrandId: {VehicleBrandId}",
            userId,
            myProfile != null,
            myProfile?.CategoryId,
            myProfile?.SubCategoryId,
            myProfile?.VehicleTypeId,
            myProfile?.VehicleBrandId);

        var otherProfiles = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p);

        _logger.LogInformation("[ActiveChats] Found {OtherProfileCount} profiles for {OtherUserCount} chat participants",
            otherProfiles.Count, otherUserIds.Count);

        var otherLocations = await _db.UserLocations
            .AsNoTracking()
            .Where(l => otherUserIds.Contains(l.UserId))
            .ToDictionaryAsync(l => l.UserId, l => l);

        // Check for existing permanent contacts (UserMatches)
        var permanentContactUserIdsList = await _db.UserMatches
            .AsNoTracking()
            .Where(m => (m.User1Id == userId && otherUserIds.Contains(m.User2Id)) ||
                        (m.User2Id == userId && otherUserIds.Contains(m.User1Id)))
            .Where(m => m.Status == Data.Entities.MatchStatus.Confirmed)
            .Select(m => m.User1Id == userId ? m.User2Id : m.User1Id)
            .ToListAsync();
        var permanentContactUserIds = permanentContactUserIdsList.ToHashSet();

        var lastMessages = await _db.Messages
            .AsNoTracking()
            .Where(m => activeChatIds.Contains(m.ChatId))
            .GroupBy(m => m.ChatId)
            .Select(g => g.OrderByDescending(m => m.Timestamp).FirstOrDefault())
            .ToListAsync();

        var lastMsgByChat = lastMessages
            .Where(m => m != null)
            .ToDictionary(m => m!.ChatId, m => m!);

        var result = new List<ActiveChatDto>();
        foreach (var chatId in activeChatIds)
        {
            var other = otherParticipants.FirstOrDefault(p => p.ChatId == chatId);
            if (other == null) continue;

            var commonInterestsCount = 0;
            if (myProfile != null && otherProfiles.TryGetValue(other.UserId, out var otherProfile))
            {
                commonInterestsCount = GetCommonInterests(myProfile, otherProfile).Count;
                _logger.LogDebug("[ActiveChats] Chat {ChatId} - My: Cat={MyCat}, Sub={MySub}, Type={MyType}, Brand={MyBrand} vs Other: Cat={OtherCat}, Sub={OtherSub}, Type={OtherType}, Brand={OtherBrand} => CommonCount={Count}",
                    chatId,
                    myProfile.CategoryId, myProfile.SubCategoryId, myProfile.VehicleTypeId, myProfile.VehicleBrandId,
                    otherProfile.CategoryId, otherProfile.SubCategoryId, otherProfile.VehicleTypeId, otherProfile.VehicleBrandId,
                    commonInterestsCount);
            }
            else
            {
                _logger.LogDebug("[ActiveChats] Chat {ChatId} - Cannot compare - MyProfile={HasMyProfile}, OtherProfile={HasOtherProfile} for user {OtherUserId}",
                    chatId, myProfile != null, otherProfiles.ContainsKey(other.UserId), other.UserId);
            }

            var chat = chats[chatId];
            lastMsgByChat.TryGetValue(chatId, out var lastMsg);

            double? distanceMiles = null;
            bool canMeet = false;
            bool isPermanentContact = permanentContactUserIds.Contains(other.UserId);
            
            if (latitude.HasValue && longitude.HasValue && otherLocations.TryGetValue(other.UserId, out var otherLoc))
            {
                var distanceMeters = HaversineMeters(latitude.Value, longitude.Value, otherLoc.CurrentLocation.Y, otherLoc.CurrentLocation.X);
                distanceMiles = MetersToMiles(distanceMeters);
                // Can meet only if within 50m AND not already permanent contacts (already met)
                canMeet = distanceMeters <= 50 && !isPermanentContact;
            }

            // Build announcement context if chat is linked to an announcement
            AnnouncementContextDto? announcementContext = null;
            if (chat.Announcement != null)
            {
                announcementContext = new AnnouncementContextDto
                {
                    AnnouncementId = chat.Announcement.AnnouncementId.ToString(),
                    Title = chat.Announcement.Title,
                    Body = chat.Announcement.Body,
                    AuthorDisplayName = MakeAnonymousDisplayName(chat.Announcement.AuthorId),
                    AuthorColor = PickColorForUser(chat.Announcement.AuthorId, _settingsProvider.Get())
                };
            }

            result.Add(new ActiveChatDto
            {
                ChatId = chatId,
                OtherAnonymousUserId = MakeAnonymousUserId(other.UserId),
                OtherDisplayName = MakeAnonymousDisplayName(other.UserId),
                OtherDisplayColor = other.DisplayColor,
                LastMessage = lastMsg?.EncryptedContent,
                LastMessageIsMe = false, // Would need to check sender
                LastMessageTimestamp = lastMsg?.Timestamp,
                ExpiresAt = chat.ExpiresAt,
                DistanceMiles = distanceMiles,
                CanMeet = canMeet,
                IsPermanentContact = isPermanentContact,
                CommonInterestsCount = commonInterestsCount,
                Announcement = announcementContext
            });
        }

        result = result.OrderByDescending(c => c.LastMessageTimestamp ?? DateTime.MinValue).ToList();

        return new ActiveChatsResponse
        {
            Count = result.Count,
            Chats = result
        };
    }

    public async Task UpdatePresenceAsync(Guid userId, string chatId, double? latitude, double? longitude, double? speedMph)
    {
        var participant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

        if (participant == null)
        {
            throw new ForbiddenException("You are not a participant of this chat");
        }

        // Update heartbeat timestamp
        participant.LastHeartbeat = DateTime.UtcNow;

        // Update location if provided
        if (latitude.HasValue && longitude.HasValue)
        {
            participant.CurrentLocation = MakePoint(longitude.Value, latitude.Value);
        }

        if (speedMph.HasValue)
        {
            participant.CurrentSpeed = (float)speedMph.Value;
        }

        await _db.SaveChangesAsync();
    }

    public async Task MarkMessagesAsReadAsync(Guid userId, string chatId)
    {
        // Get the participant to find their ParticipantId
        var participant = await _db.ChatParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

        if (participant == null)
        {
            throw new ForbiddenException("You are not a participant of this chat");
        }

        // Mark all unread messages from OTHER participants as read
        var now = DateTime.UtcNow;
        var unreadMessages = await _db.Messages
            .Where(m => m.ChatId == chatId && m.SenderId != participant.ParticipantId && m.ReadAt == null)
            .ToListAsync();

        if (unreadMessages.Count == 0) return;

        var messageIds = new List<Guid>();
        foreach (var msg in unreadMessages)
        {
            msg.ReadAt = now;
            messageIds.Add(msg.MessageId);
        }

        await _db.SaveChangesAsync();

        // Notify the sender(s) that their messages were read
        await _realtime.SendMessagesReadAsync(chatId, new
        {
            chatId,
            messageIds = messageIds.Select(id => id.ToString()).ToList(),
            readAt = now,
            readByUserId = MakeTemporaryUserId(userId, chatId)
        });
    }

    #region Private Helper Methods

    private async Task<string> GetOrCreateChatAsync(Guid userId1, Guid userId2, ChatSettings settings, Point? center, double radiusMiles, Guid? announcementId = null)
    {
        var (u1, u2) = userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);

        // Check for existing chat between these users (no expiration)
        var existingChat = await _db.Chats
            .Where(c => _db.ChatParticipants.Any(p => p.ChatId == c.ChatId && p.UserId == u1) &&
                        _db.ChatParticipants.Any(p => p.ChatId == c.ChatId && p.UserId == u2))
            .FirstOrDefaultAsync();

        if (existingChat != null)
        {
            // Update announcement if provided and not already set
            if (announcementId.HasValue && !existingChat.AnnouncementId.HasValue)
            {
                existingChat.AnnouncementId = announcementId;

                var announcement = await _db.Announcements.FindAsync(announcementId.Value);
                if (announcement != null)
                {
                    announcement.ReplyCount++;
                }
            }
            return existingChat.ChatId;
        }

        var chatId = Guid.NewGuid().ToString();
        var chat = new Chat
        {
            ChatId = chatId,
            CenterLocation = center ?? MakePoint(0, 0),
            RadiusMeters = (int)Math.Round(radiusMiles * 1609.344),
            ActiveUserCount = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(100), // Chats don't expire
            AnnouncementId = announcementId
        };
        _db.Chats.Add(chat);

        // Add both participants immediately to prevent race condition duplicates
        var defaultPoint = center ?? MakePoint(0, 0);
        _db.ChatParticipants.Add(new ChatParticipant
        {
            ParticipantId = Guid.NewGuid(),
            ChatId = chatId,
            UserId = u1,
            DisplayColor = PickColorForUser(u1, settings),
            CurrentLocation = defaultPoint,
            CurrentSpeed = 0,
            LastHeartbeat = DateTime.UtcNow,
            ProfileCategory = 0
        });
        _db.ChatParticipants.Add(new ChatParticipant
        {
            ParticipantId = Guid.NewGuid(),
            ChatId = chatId,
            UserId = u2,
            DisplayColor = PickColorForUser(u2, settings),
            CurrentLocation = defaultPoint,
            CurrentSpeed = 0,
            LastHeartbeat = DateTime.UtcNow,
            ProfileCategory = 0
        });

        if (announcementId.HasValue)
        {
            var announcement = await _db.Announcements.FindAsync(announcementId.Value);
            if (announcement != null)
            {
                announcement.ReplyCount++;
            }
        }
        await _db.SaveChangesAsync();

        return chatId;
    }

    private async Task EnsureParticipantAsync(string chatId, Guid userId, double latitude, double longitude, double speedMph, string? publicKey, ChatSettings settings)
    {
        var existing = await _db.ChatParticipants.FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        var point = MakePoint(longitude, latitude);

        var profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);

        if (existing == null)
        {
            var displayColor = PickColorForUser(userId, settings);

            _db.ChatParticipants.Add(new ChatParticipant
            {
                ParticipantId = Guid.NewGuid(),
                ChatId = chatId,
                UserId = userId,
                DisplayColor = displayColor,
                CurrentLocation = point,
                CurrentSpeed = (float)speedMph,
                ProfileCategory = 0, // Default category
                ProfileSpecialization = null,
                LastHeartbeat = DateTime.UtcNow,
                PublicKey = publicKey
            });
            return;
        }

        existing.CurrentLocation = point;
        existing.CurrentSpeed = (float)speedMph;
        existing.LastHeartbeat = DateTime.UtcNow;
        existing.ProfileCategory = 0; // Default category
        existing.ProfileSpecialization = null;
        if (!string.IsNullOrWhiteSpace(publicKey))
        {
            existing.PublicKey = publicKey;
        }
    }

    private async Task EndChatAsync(string chatId, string reason)
    {
        await _realtime.SendChatEndedAsync(chatId, new { chatId, reason });

        var messages = await _db.Messages.Where(m => m.ChatId == chatId).ToListAsync();
        _db.Messages.RemoveRange(messages);

        var participants = await _db.ChatParticipants.Where(p => p.ChatId == chatId).ToListAsync();
        _db.ChatParticipants.RemoveRange(participants);

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat != null)
        {
            _db.Chats.Remove(chat);
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpsertUserLocationAsync(Guid userId, double latitude, double longitude, double speedMph)
    {
        var point = MakePoint(longitude, latitude);
        var existing = await _db.UserLocations.FirstOrDefaultAsync(l => l.UserId == userId);
        if (existing == null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return;

            _db.UserLocations.Add(new UserLocation
            {
                UserId = userId,
                CurrentLocation = point,
                Speed = (float)speedMph,
                Timestamp = DateTime.UtcNow
            });
        }
        else
        {
            existing.CurrentLocation = point;
            existing.Speed = (float)speedMph;
            existing.Timestamp = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    private async Task<HashSet<Guid>> GetChatDndUserIdsAsync(List<Guid> userIds)
    {
        // Simplified - return empty set for now
        return new HashSet<Guid>();
    }

    private static List<string> GetCommonInterests(UserProfile profile1, UserProfile profile2)
    {
        var common = new List<string>();

        // Compare CategoryId - always required
        if (profile1.CategoryId == profile2.CategoryId)
        {
            var name = profile1.Category?.Name ?? "Same Category";
            common.Add(name);
        }

        // Compare SubCategoryId - optional
        if (profile1.SubCategoryId.HasValue && profile2.SubCategoryId.HasValue && 
            profile1.SubCategoryId == profile2.SubCategoryId)
        {
            var name = profile1.SubCategory?.Name ?? "Same SubCategory";
            common.Add(name);
        }

        // Compare VehicleTypeId - optional
        if (profile1.VehicleTypeId.HasValue && profile2.VehicleTypeId.HasValue && 
            profile1.VehicleTypeId == profile2.VehicleTypeId)
        {
            var name = profile1.VehicleType?.Name ?? "Same Vehicle Type";
            common.Add(name);
        }

        // Compare VehicleBrandId - optional
        if (profile1.VehicleBrandId.HasValue && profile2.VehicleBrandId.HasValue && 
            profile1.VehicleBrandId == profile2.VehicleBrandId)
        {
            var name = profile1.VehicleBrand?.Name ?? "Same Brand";
            common.Add(name);
        }

        return common;
    }

    private static double ClampRadiusMiles(double? requested, ChatSettings settings)
    {
        var min = settings.MinRadiusMiles;
        var max = settings.MaxRadiusMiles;
        var def = settings.DefaultRadiusMiles;
        var r = requested ?? def;
        return Math.Clamp(r, min, max);
    }

    private static double MilesToMeters(double miles) => miles * 1609.344;
    private static double MetersToMiles(double meters) => meters / 1609.344;

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private static Point MakePoint(double longitude, double latitude) => GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));

    private static string MakeAnonymousUserId(Guid userId) => "anon_" + userId.ToString("N").Substring(0, 8);

    private static string MakeAnonymousDisplayName(Guid userId)
    {
        var adjectives = new[] { "Swift", "Steady", "Rolling", "Cruising", "Roaming", "Blazing" };
        var nouns = new[] { "Hauler", "Trucker", "Driver", "Rider", "Voyager", "Wanderer" };
        var hash = userId.GetHashCode();
        return $"{adjectives[Math.Abs(hash) % adjectives.Length]} {nouns[Math.Abs(hash >> 8) % nouns.Length]}";
    }

    private static string MakeTemporaryUserId(Guid userId, string chatId)
    {
        var combined = userId.ToString("N") + chatId;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return "tmp_" + Convert.ToHexString(hash).Substring(0, 12).ToLower();
    }

    private static string PickColorForUser(Guid userId, ChatSettings settings)
    {
        var palette = (settings.RandomColorPalette ?? new List<string>())
            .Where(c => !(settings.RestrictedColors ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (!palette.Any()) return "amber";

        var bytes = System.Security.Cryptography.SHA256.HashData(userId.ToByteArray());
        var idx = bytes[0] % palette.Count;
        return palette[idx];
    }

    private bool TryParseAnonymousUserId(string anonId, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(anonId) || !anonId.StartsWith("anon_")) return false;
        
        var prefix = anonId.Substring(5); // Remove "anon_" prefix
        
        // Look up user by GUID prefix match
        var user = _db.UserLocations
            .AsEnumerable()
            .FirstOrDefault(u => u.UserId.ToString("N").StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        
        if (user != null)
        {
            userId = user.UserId;
            return true;
        }
        
        // Also check Users table in case they don't have a location yet
        var userFromUsers = _db.Users
            .AsEnumerable()
            .FirstOrDefault(u => u.Id.ToString("N").StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        
        if (userFromUsers != null)
        {
            userId = userFromUsers.Id;
            return true;
        }
        
        return false;
    }

    #endregion

    #region gRPC Support Methods

    public async Task<UserProfileResponse> UpsertProfileAsync(Guid userId, UpsertChatProfileRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request is required");
        }

        var existing = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        
        // Parse category IDs if provided
        Guid? categoryId = null;
        Guid? subCategoryId = null;
        Guid? vehicleTypeId = null;
        Guid? vehicleBrandId = null;

        if (request.CategoryIds?.Any() == true && Guid.TryParse(request.CategoryIds.First(), out var catId))
        {
            categoryId = catId;
        }
        if (request.SubCategoryIds?.Any() == true && Guid.TryParse(request.SubCategoryIds.First(), out var subCatId))
        {
            subCategoryId = subCatId;
        }
        if (!string.IsNullOrEmpty(request.VehicleTypeId) && Guid.TryParse(request.VehicleTypeId, out var vtId))
        {
            vehicleTypeId = vtId;
        }
        if (!string.IsNullOrEmpty(request.VehicleBrandId) && Guid.TryParse(request.VehicleBrandId, out var vbId))
        {
            vehicleBrandId = vbId;
        }

        if (existing == null)
        {
            // Get a default category if none provided
            if (!categoryId.HasValue)
            {
                var defaultCategory = await _db.ProfileCategories.FirstOrDefaultAsync(c => c.IsActive);
                categoryId = defaultCategory?.CategoryId;
            }

            _db.UserProfiles.Add(new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                CategoryId = categoryId ?? Guid.Empty,
                SubCategoryId = subCategoryId,
                VehicleTypeId = vehicleTypeId,
                VehicleBrandId = vehicleBrandId,
                CustomVehicleType = request.CustomVehicleType,
                CustomVehicleBrand = request.CustomVehicleBrand,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            if (categoryId.HasValue) existing.CategoryId = categoryId.Value;
            if (subCategoryId.HasValue) existing.SubCategoryId = subCategoryId;
            if (vehicleTypeId.HasValue) existing.VehicleTypeId = vehicleTypeId;
            if (vehicleBrandId.HasValue) existing.VehicleBrandId = vehicleBrandId;
            if (!string.IsNullOrEmpty(request.CustomVehicleType)) existing.CustomVehicleType = request.CustomVehicleType;
            if (!string.IsNullOrEmpty(request.CustomVehicleBrand)) existing.CustomVehicleBrand = request.CustomVehicleBrand;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return await GetProfileAsync(userId);
    }

    public async Task<IReadOnlyList<Guid>> GetChatParticipantUserIdsAsync(string chatId)
    {
        var participants = await _db.ChatParticipants
            .Where(p => p.ChatId == chatId)
            .Select(p => p.UserId)
            .ToListAsync();

        return participants;
    }

    #endregion
}
