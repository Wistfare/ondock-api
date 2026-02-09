using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Post;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class PostService : IPostService
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<PostService> _logger;
    private readonly GeometryFactory _geometryFactory;

    public PostService(OnDockDbContext db, ILogger<PostService> logger)
    {
        _db = db;
        _logger = logger;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task<PostDto> CreatePostAsync(Guid userId, CreatePostDto dto)
    {
        var location = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var post = new Data.Entities.Post
        {
            PostId = Guid.NewGuid(),
            UserId = userId,
            Location = location,
            RoadIdentification = dto.RoadIdentification,
            PostType = dto.PostType,
            MediaUrl = dto.MediaUrl,
            Description = dto.Description,
            UrgencyLevel = dto.UrgencyLevel,
            AnonymousIdentifier = GenerateAnonymousId(userId),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(dto.ExpirationHours),
            ViewCount = 0,
            ReportCount = 0,
            VideoDurationSeconds = dto.VideoDurationSeconds,
            HasAudio = dto.HasAudio
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return MapToDto(post);
    }

    public async Task<PostDto?> GetPostAsync(Guid postId)
    {
        var post = await _db.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        return post == null ? null : MapToDto(post);
    }

    public async Task<List<PostDto>> GetUserPostsAsync(Guid userId)
    {
        var posts = await _db.Posts
            .Where(p => p.UserId == userId && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    public async Task<List<PostDto>> GetPostsByUserIdsAsync(List<Guid> userIds)
    {
        var posts = await _db.Posts
            .Where(p => userIds.Contains(p.UserId) && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToDto).ToList();
    }

    public async Task<bool> DeletePostAsync(Guid postId, Guid userId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && p.UserId == userId);

        if (post == null) return false;

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IncrementViewCountAsync(Guid postId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return false;

        post.ViewCount++;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReportPostAsync(Guid postId, Guid userId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return false;

        post.ReportCount++;
        await _db.SaveChangesAsync();

        _logger.LogWarning("Post {PostId} reported by user {UserId}. Total reports: {ReportCount}", 
            postId, userId, post.ReportCount);

        return true;
    }

    public async Task ExpireOldPostsAsync(CancellationToken cancellationToken = default)
    {
        var expiredPosts = await _db.Posts
            .Where(p => p.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredPosts.Any())
        {
            _db.Posts.RemoveRange(expiredPosts);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} expired road posts", expiredPosts.Count);
        }
    }

    private static string GenerateAnonymousId(Guid userId)
    {
        var hash = userId.GetHashCode();
        return $"Trucker{Math.Abs(hash) % 10000:D4}";
    }

    private static PostDto MapToDto(Data.Entities.Post post)
    {
        return new PostDto
        {
            PostId = post.PostId,
            UserId = post.UserId,
            UserDisplayName = post.AnonymousIdentifier,
            Latitude = post.Location.Y,
            Longitude = post.Location.X,
            RoadIdentification = post.RoadIdentification,
            PostType = post.PostType,
            MediaUrl = post.MediaUrl,
            Description = post.Description,
            UrgencyLevel = post.UrgencyLevel,
            CreatedAt = post.CreatedAt,
            ExpiresAt = post.ExpiresAt,
            ViewCount = post.ViewCount,
            VideoDurationSeconds = post.VideoDurationSeconds,
            HasAudio = post.HasAudio
        };
    }
}
