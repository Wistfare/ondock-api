namespace ondock.api.DTOs.Settings;

public class UserSettingsResponse
{
    // Do Not Disturb
    public bool DndEnabled { get; set; }
    public string? DndScheduleStart { get; set; }
    public string? DndScheduleEnd { get; set; }
    public bool DndScheduleEnabled { get; set; }

    // Notifications
    public bool NotifyChat { get; set; }
    public bool NotifyAnnouncements { get; set; }
    public bool NotifyNearbyUsers { get; set; }
    public bool NotifyDockStatus { get; set; }
    public bool NotifyRoadAlerts { get; set; }
    public bool NotificationSound { get; set; }
    public bool NotificationVibration { get; set; }

    // Privacy
    public bool VisibleToNearby { get; set; }
    public bool ShareLocationInChat { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public bool ShowReadReceipts { get; set; }

    // Chat Preferences
    public double DefaultChatRadiusMiles { get; set; }
    public bool AutoJoinNearbyChats { get; set; }

    public DateTime UpdatedAt { get; set; }
}
