using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

public class UserLocation
{
    [Key]
    public Guid UserId { get; set; }
    
    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point CurrentLocation { get; set; } = null!;
    
    public float Altitude { get; set; }
    
    public float Speed { get; set; }
    
    public float Heading { get; set; }
    
    public float Accuracy { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
