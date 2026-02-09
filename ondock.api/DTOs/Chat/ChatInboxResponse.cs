namespace ondock.api.DTOs.Chat;

public class ChatInboxResponse
{
    public int Count { get; set; }
    public List<ChatInboxItemDto> Items { get; set; } = new();
}
