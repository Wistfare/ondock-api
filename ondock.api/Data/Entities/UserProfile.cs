using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    public Guid ProfileId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    // Hierarchical categorization (foreign keys)
    [Required]
    public Guid CategoryId { get; set; }

    public Guid? SubCategoryId { get; set; }

    public Guid? VehicleTypeId { get; set; }

    public Guid? VehicleBrandId { get; set; }

    // Custom values when "Others" is selected
    [MaxLength(100)]
    public string? CustomCategory { get; set; }

    [MaxLength(100)]
    public string? CustomSubCategory { get; set; }

    [MaxLength(100)]
    public string? CustomVehicleType { get; set; }

    [MaxLength(100)]
    public string? CustomVehicleBrand { get; set; }

    // Vehicle model (free text)
    [MaxLength(100)]
    public string? VehicleModel { get; set; }

    // JSON settings storage (for DND, push notifications, etc.)
    public string? Preferences { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual ProfileCategory Category { get; set; } = null!;

    [ForeignKey(nameof(SubCategoryId))]
    public virtual ProfileSubCategory? SubCategory { get; set; }

    [ForeignKey(nameof(VehicleTypeId))]
    public virtual VehicleType? VehicleType { get; set; }

    [ForeignKey(nameof(VehicleBrandId))]
    public virtual VehicleBrand? VehicleBrand { get; set; }
}
