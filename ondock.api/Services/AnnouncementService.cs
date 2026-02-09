using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly OnDockDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public AnnouncementService(OnDockDbContext db)
    {
        _db = db;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task<IReadOnlyList<AnnouncementDto>> GetAnnouncementsAsync(Guid userId, double latitude, double longitude, int take = 50)
    {
        var now = DateTime.UtcNow;
        var userLocation = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        var announcements = await _db.Announcements
            .AsNoTracking()
            .Where(a => a.ExpiresAt > now)
            .OrderByDescending(a => a.UrgencyLevel)
            .ThenByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

        return announcements.Select(a => MapToDto(a, userLocation, userId)).ToList();
    }

    public async Task<AnnouncementDto?> GetAnnouncementAsync(Guid announcementId)
    {
        var announcement = await _db.Announcements
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AnnouncementId == announcementId);

        if (announcement == null) return null;

        return MapToDto(announcement, null);
    }

    public async Task<CreateAnnouncementResponse> CreateAnnouncementAsync(Guid userId, CreateAnnouncementRequest request)
    {
        var now = DateTime.UtcNow;
        var location = _geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var announcement = new Announcement
        {
            AnnouncementId = Guid.NewGuid(),
            AuthorId = userId,
            Title = request.Title,
            Body = request.Body,
            UrgencyLevel = request.UrgencyLevel,
            OriginLocation = location,
            RadiusMiles = 5.0,
            CreatedAt = now,
            ExpiresAt = now.AddHours(24),
            IsActive = true
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        // Return the format Flutter expects
        return new CreateAnnouncementResponse
        {
            AnnouncementId = announcement.AnnouncementId.ToString(),
            RecipientCount = 0, // Will be visible to all users when they fetch announcements
            Timestamp = now
        };
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid userId, Guid announcementId)
    {
        var announcement = await _db.Announcements
            .FirstOrDefaultAsync(a => a.AnnouncementId == announcementId && a.AuthorId == userId);

        if (announcement == null) return false;

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task IncrementViewCountAsync(Guid announcementId, Guid viewerId)
    {
        var announcement = await _db.Announcements.FindAsync(announcementId);
        if (announcement == null || announcement.AuthorId == viewerId) return;
        
        announcement.ViewCount++;
        await _db.SaveChangesAsync();
    }

    public async Task IncrementReplyCountAsync(Guid announcementId)
    {
        var announcement = await _db.Announcements.FindAsync(announcementId);
        if (announcement == null) return;
        
        announcement.ReplyCount++;
        await _db.SaveChangesAsync();
    }

    private static AnnouncementDto MapToDto(Announcement a, Point? userLocation, Guid? currentUserId = null)
    {
        double distanceMiles = 0;
        if (userLocation != null && a.OriginLocation != null)
        {
            var meters = HaversineMeters(userLocation.Y, userLocation.X, a.OriginLocation.Y, a.OriginLocation.X);
            distanceMiles = meters / 1609.344;
        }

        return new AnnouncementDto
        {
            AnnouncementId = a.AnnouncementId,
            AuthorAnonymousId = $"anon_{a.AuthorId:N}".Substring(0, 20),
            AuthorDisplayName = MakeAnonymousDisplayName(a.AuthorId),
            AuthorColor = PickColorForUser(a.AuthorId),
            Title = a.Title,
            Body = a.Body,
            UrgencyLevel = a.UrgencyLevel,
            DistanceMiles = distanceMiles,
            CreatedAt = a.CreatedAt,
            ExpiresAt = a.ExpiresAt,
            ViewCount = a.ViewCount,
            ReplyCount = a.ReplyCount,
            IsMine = currentUserId.HasValue && a.AuthorId == currentUserId.Value
        };
    }

    private static string MakeAnonymousDisplayName(Guid userId)
    {
        var adjectives = new[] { "Swift", "Steady", "Rolling", "Cruising", "Roaming", "Blazing" };
        var nouns = new[] { "Hauler", "Trucker", "Driver", "Rider", "Voyager", "Wanderer" };
        var hash = userId.GetHashCode();
        var adj = adjectives[Math.Abs(hash) % adjectives.Length];
        var noun = nouns[Math.Abs(hash / adjectives.Length) % nouns.Length];
        return $"{adj} {noun}";
    }

    private static string PickColorForUser(Guid userId)
    {
        var palette = new[] { "amber", "purple", "orange", "teal", "indigo", "pink", "cyan", "lime" };
        var hash = Math.Abs(userId.GetHashCode());
        return palette[hash % palette.Length];
    }

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
}
