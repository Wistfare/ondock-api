using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Monitoring;

public class DockLightStatusResponse
{
    public Guid EventId { get; set; }
    public Guid? MonitoringSessionId { get; set; }
    public DockLightStatusType Status { get; set; }
    public string DeviceId { get; set; } = null!;
    public DockLightStatusSource Source { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? RawDetectionData { get; set; }
}
