using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Monitoring;

public class MonitoringSessionParticipantResponse
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public string Role { get; set; } = null!; // Publisher or Viewer
}
