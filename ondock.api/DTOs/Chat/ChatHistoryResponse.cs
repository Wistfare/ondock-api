namespace ondock.api.DTOs.Chat;

public class ChatHistoryResponse
{
    public string SelfTemporaryUserId { get; set; } = string.Empty;
    public List<ChatMessageResponse> Messages { get; set; } = new();
}
