namespace ondock.api.DTOs.Chat;

public class ChatInboxItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime Timestamp { get; set; }

    public string? AnnouncementId { get; set; }
    public string? FromAnonymousUserId { get; set; }
    public string? FromDisplayName { get; set; }
    public string? FromColor { get; set; }
    public Guid? ChatId { get; set; }

    // Announcement-specific fields
    public int? UrgencyLevel { get; set; }
    public double? DistanceMiles { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? ViewCount { get; set; }
    public int? ReplyCount { get; set; }
    public bool? IsMine { get; set; }
}
