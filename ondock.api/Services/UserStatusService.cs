using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Posts;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class UserStatusService : IUserStatusService
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<UserStatusService> _logger;
    private readonly IChatSettingsProvider _settingsProvider;

    public UserStatusService(
        OnDockDbContext db,
        ILogger<UserStatusService> logger,
        IChatSettingsProvider settingsProvider)
    {
        _db = db;
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    public async Task<PostDto> CreateStatusAsync(Guid userId, CreatePostRequest request)
    {
        var post = new UserPost
        {
            PostId = Guid.NewGuid(),
            UserId = userId,
            PostType = request.PostType,
            TextContent = request.TextContent,
            BackgroundColor = request.BackgroundColor,
            BackgroundImageUrl = request.BackgroundImageUrl,
            TextPositionX = request.TextPositionX,
            TextPositionY = request.TextPositionY,
            MediaUrl = request.MediaUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            DurationSeconds = request.DurationSeconds,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            ViewCount = 0,
            IsActive = true
        };

        _db.UserPosts.Add(post);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created status {PostId} of type {PostType}", 
            userId, post.PostId, post.PostType);

        return MapToDto(post, userId);
    }

    public async Task<List<PostDto>> GetMyStatusesAsync(Guid userId)
    {
        var posts = await _db.UserPosts
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(p => MapToDto(p, userId)).ToList();
    }

    public async Task<UserPostsResponse> GetUserStatusesAsync(Guid viewerUserId, string anonymousUserId)
    {
        // Parse anonymous user ID to get actual user ID
        var targetUser = await FindUserByAnonymousId(anonymousUserId);
        if (targetUser == null)
        {
            return new UserPostsResponse
            {
                UserId = anonymousUserId,
                DisplayName = "Unknown",
                Color = "#888888",
                PostCount = 0,
                Posts = new()
            };
        }

        var posts = await _db.UserPosts
            .AsNoTracking()
            .Where(p => p.UserId == targetUser.Id && p.IsActive && p.ExpiresAt > DateTime.UtcNow)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return new UserPostsResponse
        {
            UserId = anonymousUserId,
            DisplayName = MakeAnonymousDisplayName(targetUser.Id),
            Color = PickColorForUser(targetUser.Id),
            PostCount = posts.Count,
            LastPostAt = posts.LastOrDefault()?.CreatedAt,
            Posts = posts.Select(p => MapToDto(p, viewerUserId)).ToList()
        };
    }

    public async Task<List<NearbyUserWithPostsDto>> GetNearbyUsersWithStatusesAsync(
        Guid userId, 
        double latitude, 
        double longitude, 
        double radiusMiles)
    {
        var now = DateTime.UtcNow;

        // Get users with active statuses who have a recent location
        var usersWithStatuses = await _db.UserPosts
            .AsNoTracking()
            .Where(p => p.IsActive && p.ExpiresAt > now && p.UserId != userId)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync();

        // Get their locations
        var userLocations = await _db.UserLocations
            .AsNoTracking()
            .Where(l => usersWithStatuses.Contains(l.UserId))
            .ToListAsync();

        // Get view status for this user
        var viewedPostIdsList = await _db.Set<PostView>()
            .AsNoTracking()
            .Where(v => v.ViewerId == userId)
            .Select(v => v.PostId)
            .ToListAsync();
        var viewedPostIds = viewedPostIdsList.ToHashSet();

        // Get status counts per user
        var statusCounts = await _db.UserPosts
            .AsNoTracking()
            .Where(p => p.IsActive && p.ExpiresAt > now && usersWithStatuses.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), PostIds = g.Select(p => p.PostId).ToList() })
            .ToListAsync();

        var result = new List<NearbyUserWithPostsDto>();

        foreach (var location in userLocations)
        {
            var distanceMeters = HaversineMeters(
                latitude, longitude,
                location.CurrentLocation.Y, location.CurrentLocation.X);
            var distanceMiles = distanceMeters * 0.000621371;

            if (distanceMiles > radiusMiles) continue;

            var statusInfo = statusCounts.FirstOrDefault(s => s.UserId == location.UserId);
            if (statusInfo == null || statusInfo.Count == 0) continue;

            var hasUnseenPosts = statusInfo.PostIds.Any(pid => !viewedPostIds.Contains(pid));

            result.Add(new NearbyUserWithPostsDto
            {
                AnonymousUserId = MakeAnonymousUserId(location.UserId),
                DisplayName = MakeAnonymousDisplayName(location.UserId),
                Color = PickColorForUser(location.UserId),
                Latitude = location.CurrentLocation.Y,
                Longitude = location.CurrentLocation.X,
                DistanceMiles = distanceMiles,
                PostCount = statusInfo.Count,
                HasUnseenPosts = hasUnseenPosts
            });
        }

        return result.OrderBy(u => u.DistanceMiles).ToList();
    }

    public async Task<bool> DeleteStatusAsync(Guid userId, Guid postId)
    {
        var post = await _db.UserPosts
            .FirstOrDefaultAsync(p => p.PostId == postId && p.UserId == userId);

        if (post == null) return false;

        post.IsActive = false;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted status {PostId}", userId, postId);
        return true;
    }

    public async Task RecordViewAsync(Guid viewerUserId, Guid postId)
    {
        var existingView = await _db.Set<PostView>()
            .FirstOrDefaultAsync(v => v.PostId == postId && v.ViewerId == viewerUserId);

        if (existingView != null) return;

        var post = await _db.UserPosts.FindAsync(postId);
        if (post == null || !post.IsActive || post.ExpiresAt <= DateTime.UtcNow) return;

        // Don't count self-views
        if (post.UserId == viewerUserId) return;

        var view = new PostView
        {
            ViewId = Guid.NewGuid(),
            PostId = postId,
            ViewerId = viewerUserId,
            ViewedAt = DateTime.UtcNow
        };

        _db.Set<PostView>().Add(view);
        post.ViewCount++;
        await _db.SaveChangesAsync();
    }

    public async Task RecordViewsBatchAsync(Guid viewerUserId, List<Guid> postIds)
    {
        if (postIds.Count == 0) return;

        // Get existing views to avoid duplicates
        var existingViewPostIds = await _db.Set<PostView>()
            .Where(v => postIds.Contains(v.PostId) && v.ViewerId == viewerUserId)
            .Select(v => v.PostId)
            .ToListAsync();

        var newPostIds = postIds.Except(existingViewPostIds).ToList();
        if (newPostIds.Count == 0) return;

        // Get valid posts (active, not expired, not self)
        var validPosts = await _db.UserPosts
            .Where(p => newPostIds.Contains(p.PostId) && 
                       p.IsActive && 
                       p.ExpiresAt > DateTime.UtcNow && 
                       p.UserId != viewerUserId)
            .ToListAsync();

        if (validPosts.Count == 0) return;

        // Create views and increment counts
        foreach (var post in validPosts)
        {
            _db.Set<PostView>().Add(new PostView
            {
                ViewId = Guid.NewGuid(),
                PostId = post.PostId,
                ViewerId = viewerUserId,
                ViewedAt = DateTime.UtcNow
            });
            post.ViewCount++;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUserStatusCountAsync(Guid userId)
    {
        return await _db.UserPosts
            .CountAsync(p => p.UserId == userId && p.IsActive && p.ExpiresAt > DateTime.UtcNow);
    }

    public async Task CleanupExpiredStatusesAsync(CancellationToken ct = default)
    {
        var expired = await _db.UserPosts
            .Where(p => p.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (expired.Any())
        {
            _db.UserPosts.RemoveRange(expired);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Cleaned up {Count} expired user statuses", expired.Count);
        }
    }

    private async Task<User?> FindUserByAnonymousId(string anonymousUserId)
    {
        if (!anonymousUserId.StartsWith("anon_")) return null;

        var prefix = anonymousUserId.Substring(5).ToLowerInvariant();
        
        // EF Core can't translate StartsWith with StringComparison to SQL for PostgreSQL
        // The anonymous ID is generated from the first 8 chars of the GUID without dashes
        // We need to find users whose GUID starts with this prefix
        // Load user IDs and filter in memory (the prefix is 8 chars so matches should be unique)
        var allUserIds = await _db.Users.Select(u => new { u.Id }).ToListAsync();
        var matchingUserId = allUserIds
            .FirstOrDefault(u => u.Id.ToString("N").Substring(0, 8).Equals(prefix, StringComparison.OrdinalIgnoreCase));
        
        if (matchingUserId == null) return null;
        
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == matchingUserId.Id);
    }

    private PostDto MapToDto(UserPost post, Guid viewerUserId)
    {
        return new PostDto
        {
            PostId = post.PostId.ToString(),
            UserId = MakeAnonymousUserId(post.UserId),
            AuthorDisplayName = MakeAnonymousDisplayName(post.UserId),
            AuthorColor = PickColorForUser(post.UserId),
            PostType = post.PostType,
            TextContent = post.TextContent,
            BackgroundColor = post.BackgroundColor,
            BackgroundImageUrl = post.BackgroundImageUrl,
            TextPositionX = post.TextPositionX,
            TextPositionY = post.TextPositionY,
            MediaUrl = post.MediaUrl,
            ThumbnailUrl = post.ThumbnailUrl,
            DurationSeconds = post.DurationSeconds,
            CreatedAt = post.CreatedAt,
            ExpiresAt = post.ExpiresAt,
            ViewCount = post.ViewCount,
            IsMine = post.UserId == viewerUserId
        };
    }

    private static string MakeAnonymousUserId(Guid userId)
    {
        return "anon_" + userId.ToString("N").Substring(0, 8);
    }

    private static string MakeAnonymousDisplayName(Guid userId)
    {
        var hash = userId.GetHashCode();
        var adjectives = new[] { "Swift", "Steady", "Reliable", "Friendly", "Cool", "Brave", "Calm", "Sharp" };
        var nouns = new[] { "Hauler", "Trucker", "Driver", "Rider", "Mover", "Roller", "Cruiser", "Pilot" };
        var adj = adjectives[Math.Abs(hash) % adjectives.Length];
        var noun = nouns[Math.Abs(hash / adjectives.Length) % nouns.Length];
        var num = Math.Abs(hash % 1000);
        return $"{adj} {noun} {num}";
    }

    private string PickColorForUser(Guid userId)
    {
        var settings = _settingsProvider.Get();
        var colors = settings.RandomColorPalette;
        if (colors == null || colors.Count == 0)
        {
            colors = new List<string> { "#FF5722", "#2196F3", "#4CAF50", "#9C27B0", "#FF9800" };
        }
        var index = Math.Abs(userId.GetHashCode()) % colors.Count;
        return colors[index];
    }

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
}
