using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Monitoring;

public class StartMonitoringSessionRequest
{
    [Required]
    [MaxLength(100)]
    public string InitiatorDeviceId { get; set; } = null!;

    [MaxLength(500)]
    public string? Metadata { get; set; }
}
