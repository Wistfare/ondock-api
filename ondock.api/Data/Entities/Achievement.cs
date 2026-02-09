using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum AchievementType
{
    Distance,
    Social,
    Usage,
    Community
}

public class Achievement
{
    [Key]
    public Guid AchievementId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public AchievementType Type { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public int Progress { get; set; }
    
    public int Target { get; set; }
    
    public DateTime? UnlockedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
