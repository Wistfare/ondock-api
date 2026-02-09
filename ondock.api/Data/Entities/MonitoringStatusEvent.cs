using System.ComponentModel.DataAnnotations;

namespace ondock.api.Data.Entities;

public class MonitoringStatusEvent
{
    [Key]
    public Guid EventId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    // 0 = No light, 1 = Loading (Red), 2 = Completed (Green)
    public int Status { get; set; }

    public string? RawDetectionData { get; set; }

    public DateTime CreatedAt { get; set; }
}
