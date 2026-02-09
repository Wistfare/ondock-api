using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum Platform
{
    Android,
    iOS
}

public class DeviceToken
{
    [Key]
    public Guid TokenId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    public Platform Platform { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; }
    
    public DateTime RegisteredAt { get; set; }
    
    public DateTime LastUsedAt { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
