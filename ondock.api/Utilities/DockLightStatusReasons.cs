namespace ondock.api.Utilities;

public static class DockLightStatusReasons
{
    public const string DeviceHeartbeatTimeout = "device_heartbeat_timeout";
    public const string DeviceInactive = "device_inactive";
    public const string ParticipantAbsent = "participant_absent";
    public const string ParticipantDisconnected = "participant_disconnected";
    public const string RoomClosed = "room_closed";
    public const string LiveKitUnreachable = "livekit_unreachable";
    public const string NetworkRecovered = "network_recovered";
    public const string SystemDegradedNotification = "system_degraded_notification";
    public const string FlappingDetected = "flapping_detected";
    public const string MaintenanceWindowStart = "maintenance_window_start";
    public const string MaintenanceWindowEnd = "maintenance_window_end";
    public const string Recovered = "recovered";
}