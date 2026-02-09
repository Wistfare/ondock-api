using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Posts;

public class CreatePostRequest
{
    public StatusPostType PostType { get; set; }
    public string? TextContent { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public float? TextPositionX { get; set; }
    public float? TextPositionY { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
}

public class PostDto
{
    public string PostId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string AuthorColor { get; set; } = string.Empty;
    public StatusPostType PostType { get; set; }
    public string? TextContent { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public float? TextPositionX { get; set; }
    public float? TextPositionY { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public bool IsMine { get; set; }
}

public class UserPostsResponse
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public DateTime? LastPostAt { get; set; }
    public List<PostDto> Posts { get; set; } = new();
}

public class NearbyUserWithPostsDto
{
    public string AnonymousUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMiles { get; set; }
    public int PostCount { get; set; }
    public bool HasUnseenPosts { get; set; }
}

public class UploadMediaResponse
{
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
}
