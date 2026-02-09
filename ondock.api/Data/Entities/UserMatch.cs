using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum MatchStatus
{
    Pending = 0,
    Confirmed = 1,
    Declined = 2,
    Expired = 3
}

[Table("UserMatches")]
public class UserMatch
{
    [Key]
    public Guid MatchId { get; set; }

    [Required]
    public Guid User1Id { get; set; }

    [Required]
    public Guid User2Id { get; set; }

    // QR Code flow
    [MaxLength(50)]
    public string? MatchCode { get; set; }

    [MaxLength(500)]
    public string? QRCodeHash { get; set; }

    // State
    public MatchStatus Status { get; set; } = MatchStatus.Pending;

    public bool User1Revealed { get; set; }

    public bool User2Revealed { get; set; }

    // Chat linkage
    [MaxLength(50)]
    public string? ChatId { get; set; }

    // Timestamps
    public DateTime InitiatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(User1Id))]
    public virtual User User1 { get; set; } = null!;

    [ForeignKey(nameof(User2Id))]
    public virtual User User2 { get; set; } = null!;

    [ForeignKey(nameof(ChatId))]
    public virtual Chat? Chat { get; set; }
}
