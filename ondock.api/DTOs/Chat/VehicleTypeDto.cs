namespace ondock.api.DTOs.Chat;

public class VehicleTypeDto
{
    public Guid VehicleTypeId { get; set; }
    public Guid? SubCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOther { get; set; }
}
