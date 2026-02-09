using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("ProfileSubCategories")]
public class ProfileSubCategory
{
    [Key]
    public Guid SubCategoryId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsOther { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public virtual ProfileCategory Category { get; set; } = null!;

    public virtual ICollection<VehicleType> VehicleTypes { get; set; } = new List<VehicleType>();
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
