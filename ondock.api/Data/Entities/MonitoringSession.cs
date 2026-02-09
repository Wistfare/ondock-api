using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public class MonitoringSession
{
    [Key]
    public Guid SessionId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string InitiatorDeviceId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public bool IsStreaming { get; set; }

    [MaxLength(2000)]
    public string? Metadata { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<MonitoringSessionParticipant> Participants { get; set; } = new List<MonitoringSessionParticipant>();
    public virtual ICollection<DockLightStatusEvent> Events { get; set; } = new List<DockLightStatusEvent>();
}
