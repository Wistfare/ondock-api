namespace ondock.api.DTOs.Chat;

public class JoinChatResponse
{
    public string ChatId { get; set; } = string.Empty;

    public string SelfTemporaryUserId { get; set; } = string.Empty;
    public string SelfDisplayColor { get; set; } = string.Empty;
    public string SelfDisplayName { get; set; } = string.Empty;

    public string OtherTemporaryUserId { get; set; } = string.Empty;
    public string OtherDisplayColor { get; set; } = string.Empty;
    public string OtherDisplayName { get; set; } = string.Empty;
    public Guid? OtherUserId { get; set; } // Internal user ID for broadcasting

    public DateTime ExpiresAt { get; set; }
}
