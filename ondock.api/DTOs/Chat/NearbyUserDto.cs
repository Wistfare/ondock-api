using ondock.api.DTOs.Post;

namespace ondock.api.DTOs.Chat;

public class NearbyUserDto
{
    public string AnonymousUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMiles { get; set; }

    public bool HasActiveChat { get; set; }
    public string? ChatId { get; set; }

    // Interest matching
    public int CommonInterestsCount { get; set; }
    public List<string> CommonInterests { get; set; } = new();

    // Last activity
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }

    // Posts - for avatar indicator and content
    public int PostCount { get; set; }
    public bool HasRecentPost { get; set; }
    public List<PostDto> Posts { get; set; } = new();
}
