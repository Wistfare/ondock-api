using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("ChatKeyExchanges")]
public class ChatKeyExchange
{
    [Key]
    public Guid ExchangeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public Guid FromUserId { get; set; }

    [Required]
    public Guid ToUserId { get; set; }

    [Required]
    public string EncryptedSessionKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ChatId))]
    public virtual Chat Chat { get; set; } = null!;

    [ForeignKey(nameof(FromUserId))]
    public virtual User FromUser { get; set; } = null!;

    [ForeignKey(nameof(ToUserId))]
    public virtual User ToUser { get; set; } = null!;
}
