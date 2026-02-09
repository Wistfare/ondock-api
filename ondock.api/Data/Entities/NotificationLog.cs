using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum NotificationType
{
    DockStatusChanged,
    NewChatMessage,
    RoadConditionAlert,
    AchievementUnlocked,
    SystemAnnouncement
}

public enum DeliveryStatus
{
    Sent,
    Delivered,
    Failed
}

public class NotificationLog
{
    [Key]
    public Guid NotificationId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public NotificationType Type { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Body { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? Data { get; set; }
    
    public DateTime SentAt { get; set; }
    
    public int DeviceCount { get; set; }
    
    public DeliveryStatus DeliveryStatus { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
