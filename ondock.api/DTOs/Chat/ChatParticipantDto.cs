namespace ondock.api.DTOs.Chat;

public class ChatParticipantDto
{
    public string AnonymousUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayColor { get; set; } = string.Empty;
    public double? DistanceMiles { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public ProfileCategoryDto? Category { get; set; }
    public ProfileSubCategoryDto? SubCategory { get; set; }
}
