using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class AnnouncementDto
{
    public Guid AnnouncementId { get; set; }
    public string AuthorAnonymousId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string AuthorColor { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public UrgencyLevel UrgencyLevel { get; set; }
    public double DistanceMiles { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public int ReplyCount { get; set; }
    public bool IsMine { get; set; }
}
