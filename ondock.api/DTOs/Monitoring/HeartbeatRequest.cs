using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Monitoring;

public class HeartbeatRequest
{
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = null!;
}