namespace ondock.api.DTOs.Chat;

public class VehicleBrandDto
{
    public Guid BrandId { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOther { get; set; }
}
