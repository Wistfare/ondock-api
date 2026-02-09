using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Monitoring;

public class MonitoringSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string InitiatorDeviceId { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Metadata { get; set; }
    public bool IsStreaming { get; set; }

    public List<MonitoringSessionParticipantResponse> Participants { get; set; } = new();
    public List<DockLightStatusResponse> Events { get; set; } = new();
}
