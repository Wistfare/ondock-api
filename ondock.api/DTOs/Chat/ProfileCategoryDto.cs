namespace ondock.api.DTOs.Chat;

public class ProfileCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOther { get; set; }
}
