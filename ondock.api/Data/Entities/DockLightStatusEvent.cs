using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum DockLightStatusType
{
    Red = 0,
    Green = 1,
    Obstacle = 2,
    ConnectionLost = 3
}

public enum DockLightStatusSource
{
    Device = 0,
    LiveKit = 1,
    System = 2
}

// Monitoring device identity now tracked by a unique DeviceId (Guid) rather than a type enum.

public class DockLightStatusEvent
{
    [Key]
    public Guid EventId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    // Monitoring session this event belongs to
    public Guid? MonitoringSessionId { get; set; }

    [Required]
    public DockLightStatusType Status { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = null!;

    [Required]
    public DockLightStatusSource Source { get; set; } = DockLightStatusSource.Device;

    // Optional short reason or origin detail (e.g. "participant_disconnected", "webhook_connection_state")
    [MaxLength(200)]
    public string? Reason { get; set; }

    public DateTime Timestamp { get; set; }

    // Optional raw payload / debug info from client (e.g., RGB values, confidence score)
    [MaxLength(1000)]
    public string? RawDetectionData { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public MonitoringSession? MonitoringSession { get; set; }
}
