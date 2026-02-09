namespace ondock.api.DTOs.Chat;

public class ProfileSubCategoryDto
{
    public Guid SubCategoryId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOther { get; set; }
}
