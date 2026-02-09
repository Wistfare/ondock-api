using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("VehicleBrands")]
public class VehicleBrand
{
    [Key]
    public Guid BrandId { get; set; }

    public Guid? VehicleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsOther { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(VehicleTypeId))]
    public virtual VehicleType? VehicleType { get; set; }

    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
