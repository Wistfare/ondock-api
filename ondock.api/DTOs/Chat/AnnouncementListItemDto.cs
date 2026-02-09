using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class AnnouncementListItemDto
{
    public Guid AnnouncementId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string AuthorColor { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public UrgencyLevel UrgencyLevel { get; set; }
    public double DistanceMiles { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReplyCount { get; set; }
    public bool IsMine { get; set; }
}
