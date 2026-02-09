using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum RewardType
{
    Discount,
    Feature,
    Customization
}

public class Reward
{
    [Key]
    public Guid RewardId { get; set; }
    
    public RewardType Type { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? Value { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? RequiredAchievements { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserReward> UserRewards { get; set; } = new List<UserReward>();
}
