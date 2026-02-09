using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum StatusPostType
{
    Text = 0,
    Image = 1,
    Video = 2
}

[Table("UserPosts")]
public class UserPost
{
    [Key]
    public Guid PostId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public StatusPostType PostType { get; set; }

    /// <summary>
    /// Text content for text posts, or caption for media posts
    /// </summary>
    [MaxLength(1000)]
    public string? TextContent { get; set; }

    /// <summary>
    /// Background color for text posts (hex color code)
    /// </summary>
    [MaxLength(10)]
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Background image URL for text posts
    /// </summary>
    [MaxLength(500)]
    public string? BackgroundImageUrl { get; set; }

    /// <summary>
    /// Text position X (0-1 normalized)
    /// </summary>
    public float? TextPositionX { get; set; }

    /// <summary>
    /// Text position Y (0-1 normalized)
    /// </summary>
    public float? TextPositionY { get; set; }

    /// <summary>
    /// Media URL for image/video posts
    /// </summary>
    [MaxLength(500)]
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Thumbnail URL for video posts
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Duration in seconds for video posts
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// When the post was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the post expires (24 hours after creation by default)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of views
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Is the post still active (not deleted)
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<PostView> Views { get; set; } = new List<PostView>();
}

[Table("PostViews")]
public class PostView
{
    [Key]
    public Guid ViewId { get; set; }

    [Required]
    public Guid PostId { get; set; }

    [Required]
    public Guid ViewerId { get; set; }

    public DateTime ViewedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(PostId))]
    public virtual UserPost Post { get; set; } = null!;

    [ForeignKey(nameof(ViewerId))]
    public virtual User Viewer { get; set; } = null!;
}
