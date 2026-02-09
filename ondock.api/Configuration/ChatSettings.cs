namespace ondock.api.Configuration;

public class ChatSettings
{
    public double DefaultRadiusMiles { get; set; } = 1.0;
    public double MinRadiusMiles { get; set; } = 0.1;
    public double MaxRadiusMiles { get; set; } = 1.0;
    public double SpeedThresholdMph { get; set; } = 5.0;
    public int RoomExpiryMinutes { get; set; } = 60;

    public int PresenceMaxAgeSeconds { get; set; } = 120;

    public bool DeleteRoomOnAnyExit { get; set; } = true;

    public string NearbyUserColor { get; set; } = "green";

    public List<string> RestrictedColors { get; set; } = new() { "red", "blue" };

    public List<string> RandomColorPalette { get; set; } = new()
    {
        "amber",
        "purple",
        "orange",
        "teal",
        "indigo",
        "pink",
        "cyan",
        "lime"
    };

    /// <summary>
    /// When true, bypasses speed, presence, and distance restrictions for nearby user discovery.
    /// All registered users with location data will be visible regardless of their speed or last update time.
    /// </summary>
    public bool BypassNearbyRestrictions { get; set; } = false;
}
