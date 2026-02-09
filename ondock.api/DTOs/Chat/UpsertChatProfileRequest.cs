namespace ondock.api.DTOs.Chat;

public class UpsertChatProfileRequest
{
    public string? DisplayName { get; set; }
    public string? ColorName { get; set; }
    public List<string>? CategoryIds { get; set; }
    public List<string>? SubCategoryIds { get; set; }
    public string? VehicleTypeId { get; set; }
    public string? VehicleBrandId { get; set; }
    public string? CustomVehicleType { get; set; }
    public string? CustomVehicleBrand { get; set; }
}
