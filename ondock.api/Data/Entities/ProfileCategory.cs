using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

[Table("ProfileCategories")]
public class ProfileCategory
{
    [Key]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsOther { get; set; } = false;

    // Navigation properties
    public virtual ICollection<ProfileSubCategory> SubCategories { get; set; } = new List<ProfileSubCategory>();
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
