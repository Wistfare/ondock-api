using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

public enum PostType
{
    Video,
    Photo,
    Text
}

public enum UrgencyLevel
{
    Low,
    Medium,
    High,
    Critical
}

[Table("Posts")]
public class Post
{
    [Key]
    public Guid PostId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point Location { get; set; } = null!;
    
    [MaxLength(100)]
    public string? RoadIdentification { get; set; }
    
    public PostType PostType { get; set; }
    
    [MaxLength(500)]
    public string? MediaUrl { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public UrgencyLevel UrgencyLevel { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AnonymousIdentifier { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public int ViewCount { get; set; }
    
    public int ReportCount { get; set; }
    
    public int VideoDurationSeconds { get; set; }
    
    public bool HasAudio { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
