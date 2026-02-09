using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class UpsertUserProfileRequest
{
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
    public string? CustomBrand { get; set; }
}
