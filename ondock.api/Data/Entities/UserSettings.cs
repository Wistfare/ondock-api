using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

/// <summary>
/// User settings entity for storing all user preferences and configuration.
/// Consolidates DND, notification preferences, privacy settings, etc.
/// </summary>
[Table("UserSettings")]
public class UserSettings
{
    [Key]
    public Guid SettingsId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    #region Do Not Disturb

    /// <summary>
    /// Whether DND mode is currently enabled
    /// </summary>
    public bool DndEnabled { get; set; }

    /// <summary>
    /// Optional: DND schedule start time (e.g., "22:00" for quiet hours)
    /// </summary>
    [MaxLength(5)]
    public string? DndScheduleStart { get; set; }

    /// <summary>
    /// Optional: DND schedule end time (e.g., "07:00" for quiet hours)
    /// </summary>
    [MaxLength(5)]
    public string? DndScheduleEnd { get; set; }

    /// <summary>
    /// Whether to use scheduled DND (quiet hours)
    /// </summary>
    public bool DndScheduleEnabled { get; set; }

    #endregion

    #region Notifications

    /// <summary>
    /// Enable push notifications for chat messages
    /// </summary>
    public bool NotifyChat { get; set; } = true;

    /// <summary>
    /// Enable push notifications for announcements
    /// </summary>
    public bool NotifyAnnouncements { get; set; } = true;

    /// <summary>
    /// Enable push notifications for nearby users
    /// </summary>
    public bool NotifyNearbyUsers { get; set; } = true;

    /// <summary>
    /// Enable push notifications for dock status changes
    /// </summary>
    public bool NotifyDockStatus { get; set; } = true;

    /// <summary>
    /// Enable push notifications for road intelligence alerts
    /// </summary>
    public bool NotifyRoadAlerts { get; set; } = true;

    /// <summary>
    /// Enable sound for notifications
    /// </summary>
    public bool NotificationSound { get; set; } = true;

    /// <summary>
    /// Enable vibration for notifications
    /// </summary>
    public bool NotificationVibration { get; set; } = true;

    #endregion

    #region Privacy

    /// <summary>
    /// Whether to show user on nearby users list
    /// </summary>
    public bool VisibleToNearby { get; set; } = true;

    /// <summary>
    /// Whether to share location with chat participants
    /// </summary>
    public bool ShareLocationInChat { get; set; } = true;

    /// <summary>
    /// Whether to show online status to others
    /// </summary>
    public bool ShowOnlineStatus { get; set; } = true;

    /// <summary>
    /// Whether to show read receipts to others
    /// </summary>
    public bool ShowReadReceipts { get; set; } = true;

    #endregion

    #region Chat Preferences

    /// <summary>
    /// Default chat radius in miles (0.1 to 1.0)
    /// </summary>
    public double DefaultChatRadiusMiles { get; set; } = 0.5;

    /// <summary>
    /// Auto-join nearby chats when stationary
    /// </summary>
    public bool AutoJoinNearbyChats { get; set; }

    #endregion

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
