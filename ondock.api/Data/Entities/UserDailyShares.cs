using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ondock.api.Data.Entities;

[PrimaryKey(nameof(UserId), nameof(Date))]
public class UserDailyShares
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public DateOnly Date { get; set; }
    
    public int ShareCount { get; set; }
    
    public int MaxSharesPerDay { get; set; } = 10;
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
