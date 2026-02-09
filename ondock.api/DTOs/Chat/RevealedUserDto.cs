namespace ondock.api.DTOs.Chat;

public class RevealedUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public UserProfileResponse? Profile { get; set; }
}
