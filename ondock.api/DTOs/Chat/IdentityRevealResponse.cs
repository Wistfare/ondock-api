namespace ondock.api.DTOs.Chat;

public class IdentityRevealResponse
{
    public string ChatId { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public Guid TargetUserId { get; set; }
    public bool RequesterRevealed { get; set; }
    public bool TargetRevealed { get; set; }
    public bool BothRevealed { get; set; }
    public RevealedIdentityDto? RequesterIdentity { get; set; }
    public RevealedIdentityDto? TargetIdentity { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class RevealedIdentityDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
}
