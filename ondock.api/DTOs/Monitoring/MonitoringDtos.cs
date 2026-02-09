namespace ondock.api.DTOs.Monitoring;

// Note: StartMonitoringSessionRequest, MonitoringSessionResponse, and JoinMonitoringSessionRequest
// are now defined in their own separate files with proper validation attributes

public class MonitoringStatusRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? RawDetectionData { get; set; }
}

public class RequestStreamRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

public class UpdateStreamStateRequest
{
    public bool IsStreaming { get; set; }
}

public class LiveKitTokenResponse
{
    public string Token { get; set; } = string.Empty;
}
