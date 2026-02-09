namespace ondock.api.DTOs.Chat;

public class ChatResponse
{
    public Guid ChatId { get; set; }
    public string SelfTemporaryUserId { get; set; } = string.Empty;
    public string SelfDisplayName { get; set; } = string.Empty;
    public string SelfDisplayColor { get; set; } = string.Empty;
    public string OtherTemporaryUserId { get; set; } = string.Empty;
    public string OtherDisplayName { get; set; } = string.Empty;
    public string OtherDisplayColor { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int CommonInterestsCount { get; set; }
    public List<string> CommonInterests { get; set; } = new();
}
