using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

[Table("Chats")]
public class Chat
{
    [Key]
    [MaxLength(50)]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point CenterLocation { get; set; } = null!;

    [Column("Radius")]
    public int RadiusMeters { get; set; } = 1609; // ~1 mile default in meters

    public int ActiveUserCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Optional reference to the announcement this chat was created from
    /// </summary>
    public Guid? AnnouncementId { get; set; }

    // Computed property for active status (not in DB)
    [NotMapped]
    public bool IsActive => ExpiresAt > DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(AnnouncementId))]
    public virtual Announcement? Announcement { get; set; }

    public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
