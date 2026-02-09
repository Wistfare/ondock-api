using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class LocationService : ILocationService
{
    private readonly OnDockDbContext _db;
    private readonly IChatSettingsProvider _settingsProvider;
    private readonly ILogger<LocationService> _logger;

    public LocationService(OnDockDbContext db, IChatSettingsProvider settingsProvider, ILogger<LocationService> logger)
    {
        _db = db;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task<NearbyUsersResponse> GetNearbyUsersAsync(Guid userId, double latitude, double longitude, double speedMph, double? radiusMiles)
    {
        var settings = _settingsProvider.Get();
        var effectiveRadiusMiles = ClampRadiusMiles(radiusMiles, settings);
        var bypassRestrictions = settings.BypassNearbyRestrictions;

        await UpdateUserLocationAsync(userId, latitude, longitude, speedMph);

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

        _logger.LogInformation("[NearbyUsers] User {UserId} at ({Lat}, {Lng}): Found {Count} other user locations in DB. BypassRestrictions={Bypass}",
            userId, latitude, longitude, nearby.Count, bypassRestrictions);

        var withinQuery = nearby
            .Select(x => new
            {
                x.UserId,
                Latitude = x.CurrentLocation.Y,
                Longitude = x.CurrentLocation.X,
                DistanceMeters = HaversineMeters(latitude, longitude, x.CurrentLocation.Y, x.CurrentLocation.X),
                Speed = (double)x.Speed,
                x.Timestamp
            });

        if (!bypassRestrictions)
        {
            withinQuery = withinQuery.Where(x => x.DistanceMeters <= maxMeters && x.Speed <= settings.SpeedThresholdMph);
        }

        var within = withinQuery
            .OrderBy(x => x.DistanceMeters)
            .Take(100)
            .ToList();

        _logger.LogInformation("[NearbyUsers] User {UserId}: After distance/speed filter, {Count} users within range",
            userId, within.Count);

        if (!within.Any())
        {
            _logger.LogInformation("[NearbyUsers] User {UserId}: No users within range, returning empty", userId);
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
            var lastMessages = await _db.Messages
                .Where(m => activeChatIds.Contains(m.ChatId))
                .GroupBy(m => m.ChatId)
                .Select(g => new
                {
                    ChatId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToListAsync();

            foreach (var lm in lastMessages)
            {
                if (lm.LastMessage != null)
                {
                    lastMessagesByChat[lm.ChatId] = (lm.LastMessage.EncryptedContent, lm.LastMessage.Timestamp);
                }
            }
        }

        // Build result
        var resultUsers = new List<NearbyUserDto>();
        foreach (var u in within)
        {
            var hasChat = hasChatByUser.TryGetValue(u.UserId, out var chatId);
            string? lastMessage = null;
            DateTime? lastMessageTime = null;

            if (hasChat && chatId != null && lastMessagesByChat.TryGetValue(chatId, out var msgInfo))
            {
                lastMessage = msgInfo.Content;
                lastMessageTime = msgInfo.Timestamp;
            }

            // Consider online if location updated within last 5 minutes
            var isOnline = u.Timestamp >= DateTime.UtcNow.AddMinutes(-5);
            
            resultUsers.Add(new NearbyUserDto
            {
                AnonymousUserId = MakeAnonymousUserId(u.UserId),
                DisplayName = MakeAnonymousDisplayName(u.UserId),
                Color = hasChat ? PickColorForUser(u.UserId, settings) : settings.NearbyUserColor,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                DistanceMiles = MetersToMiles(u.DistanceMeters),
                HasActiveChat = hasChat,
                ChatId = chatId,
                LastMessage = lastMessage,
                LastMessageTimestamp = lastMessageTime,
                LastSeenAt = u.Timestamp,
                IsOnline = isOnline
            });
        }

        return new NearbyUsersResponse
        {
            Count = resultUsers.Count,
            Users = resultUsers
        };
    }

    public async Task UpdateUserLocationAsync(Guid userId, double latitude, double longitude, double speedMph)
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

    private static double ClampRadiusMiles(double? requested, ChatSettings settings)
    {
        if (requested == null || requested <= 0) return settings.DefaultRadiusMiles;
        return Math.Min(requested.Value, settings.MaxRadiusMiles);
    }

    private static double MilesToMeters(double miles) => miles * 1609.344;
    private static double MetersToMiles(double meters) => meters / 1609.344;

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180;

    private static Point MakePoint(double longitude, double latitude)
    {
        return new Point(longitude, latitude) { SRID = 4326 };
    }

    private static string MakeAnonymousUserId(Guid userId) => "anon_" + userId.ToString("N").Substring(0, 8);

    private static string MakeAnonymousDisplayName(Guid userId)
    {
        var adjectives = new[] { "Swift", "Steady", "Rolling", "Cruising", "Roaming", "Blazing" };
        var nouns = new[] { "Hauler", "Trucker", "Driver", "Rider", "Voyager", "Wanderer" };
        var hash = userId.GetHashCode();
        var adj = adjectives[Math.Abs(hash) % adjectives.Length];
        var noun = nouns[Math.Abs(hash / adjectives.Length) % nouns.Length];
        return $"{adj} {noun}";
    }

    private static string PickColorForUser(Guid userId, ChatSettings settings)
    {
        var palette = settings.RandomColorPalette;
        if (palette == null || palette.Count == 0) return "grey";
        var hash = Math.Abs(userId.GetHashCode());
        return palette[hash % palette.Count];
    }
}
