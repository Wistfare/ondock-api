using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

[Table("ChatParticipants")]
public class ChatParticipant
{
    [Key]
    public Guid ParticipantId { get; set; }

    [Required]
    [Column("RoomId")]
    [MaxLength(50)]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DisplayColor { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point CurrentLocation { get; set; } = null!;

    public float CurrentSpeed { get; set; }

    public DateTime LastHeartbeat { get; set; }

    // Profile snapshot for interest matching display
    public int ProfileCategory { get; set; }
    public int? ProfileSpecialization { get; set; }

    // E2E Encryption
    [MaxLength(2000)]
    public string? PublicKey { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ChatId))]
    public virtual Chat Chat { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
