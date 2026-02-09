using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("UserEncryptionKeys")]
public class UserEncryptionKey
{
    [Key]
    public Guid KeyId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string KeyType { get; set; } = "RSA-2048";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
