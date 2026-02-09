using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public class MonitoringSessionParticipant
{
    [Key]
    public Guid ParticipantId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty; // publisher or viewer

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    [ForeignKey(nameof(SessionId))]
    public virtual MonitoringSession Session { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
