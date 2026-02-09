namespace ondock.api.DTOs.Monitoring;

public class JoinMonitoringSessionRequest
{
    public string DeviceId { get; set; } = null!; // Device seeking to view an existing monitoring session
}
