using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

public enum BadgeColor
{
    Red,
    Blue,
    Both,
    None
}

public class ActiveTruck
{
    [Key]
    public Guid TruckId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point CurrentLocation { get; set; } = null!;
    
    public float CurrentSpeed { get; set; }
    
    public BadgeColor BadgeColor { get; set; }
    
    public DateTime LastMovementAt { get; set; }
    
    public int StoppedDuration { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
