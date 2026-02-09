namespace ondock.api.DTOs.Chat;

public class ReplyToAnnouncementResponse
{
    public Guid ReplyId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }
}
