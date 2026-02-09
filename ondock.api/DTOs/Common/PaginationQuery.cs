namespace ondock.api.DTOs.Common;

public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 25;

    public int Page { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    public string? Sort { get; set; }
    public string? Fields { get; set; }
}
