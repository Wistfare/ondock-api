using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class UserMatchDto
{
    public Guid MatchId { get; set; }
    public string OtherUserAnonymousId { get; set; } = string.Empty;
    public string OtherUserDisplayName { get; set; } = string.Empty;
    public MatchStatus Status { get; set; }
    public bool SelfRevealed { get; set; }
    public bool OtherRevealed { get; set; }
    public Guid? ChatId { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
