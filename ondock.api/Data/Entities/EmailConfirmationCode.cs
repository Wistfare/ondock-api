using System.ComponentModel.DataAnnotations;

namespace ondock.api.Data.Entities;

public class EmailConfirmationCode
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}
