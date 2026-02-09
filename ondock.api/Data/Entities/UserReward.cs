using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public class UserReward
{
    [Key]
    public Guid UserRewardId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid RewardId { get; set; }
    
    public DateTime ClaimedAt { get; set; }
    
    public DateTime? RedeemedAt { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(RewardId))]
    public virtual Reward Reward { get; set; } = null!;
}
