namespace ondock.api.DTOs.Chat;

public class ActiveChatsResponse
{
    public int Count { get; set; }
    public List<ActiveChatDto> Chats { get; set; } = new();
}

public class ActiveChatDto
{
    public string ChatId { get; set; } = string.Empty;
    public string OtherAnonymousUserId { get; set; } = string.Empty;
    public string OtherDisplayName { get; set; } = string.Empty;
    public string OtherDisplayColor { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public bool LastMessageIsMe { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public double? DistanceMiles { get; set; }
    public bool CanMeet { get; set; }
    public bool IsPermanentContact { get; set; }

    public int CommonInterestsCount { get; set; }
    
    /// <summary>
    /// Announcement info if this chat was created from an announcement reply
    /// </summary>
    public AnnouncementContextDto? Announcement { get; set; }
}

public class AnnouncementContextDto
{
    public string AnnouncementId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string AuthorColor { get; set; } = string.Empty;
}
