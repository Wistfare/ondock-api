using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

[Table("Announcements")]
public class Announcement
{
    [Key]
    public Guid AnnouncementId { get; set; }

    [Required]
    public Guid AuthorId { get; set; }

    // Content
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;

    // Location context (where it was created)
    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point OriginLocation { get; set; } = null!;

    public double RadiusMiles { get; set; } = 1;

    // Lifecycle
    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Stats
    public int ViewCount { get; set; }

    public int ReplyCount { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AuthorId))]
    public virtual User Author { get; set; } = null!;

    public virtual ICollection<Message> Replies { get; set; } = new List<Message>();
}
