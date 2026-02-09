using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Monitoring;

public class DockLightStatusUpdateRequest
{
    [Required]
    public DockLightStatusType Status { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = null!;

    // Optional diagnostic payload from client (e.g. camera frame analysis summary)
    [MaxLength(1000)]
    public string? RawDetectionData { get; set; }
}
