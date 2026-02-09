namespace ondock.api.DTOs.Chat;

public class CreateAnnouncementResponse
{
    public string AnnouncementId { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTime Timestamp { get; set; }
}
