namespace ondock.api.DTOs.Chat;

public class UserProfileResponse
{
    public bool HasProfile { get; set; }
    public ProfileCategoryDto? Category { get; set; }
    public ProfileSubCategoryDto? SubCategory { get; set; }
    public VehicleTypeDto? VehicleType { get; set; }
    public VehicleBrandDto? VehicleBrand { get; set; }
}
