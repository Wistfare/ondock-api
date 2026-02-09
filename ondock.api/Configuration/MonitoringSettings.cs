namespace ondock.api.Configuration;

public class MonitoringSettings
{
    public int HeartbeatTimeoutSeconds { get; set; } = 75; // gap after which device considered disconnected
    public int DataInactivityTimeoutSeconds { get; set; } = 120; // no status events nor heartbeat
    public int ParticipantAbsenceTimeoutSeconds { get; set; } = 40; // missing LiveKit participant beyond this
    public int LiveKitFailureThreshold { get; set; } = 3; // consecutive failed health probes
    public int LiveKitHealthPollSeconds { get; set; } = 60; // health probe interval
}
