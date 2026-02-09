using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("VehicleTypes")]
public class VehicleType
{
    [Key]
    public Guid VehicleTypeId { get; set; }

    public Guid? SubCategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsOther { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(SubCategoryId))]
    public virtual ProfileSubCategory? SubCategory { get; set; }

    public virtual ICollection<VehicleBrand> VehicleBrands { get; set; } = new List<VehicleBrand>();
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
