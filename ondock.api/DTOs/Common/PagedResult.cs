namespace ondock.api.DTOs.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public PaginationMetadata Pagination { get; set; } = null!;
    public PaginationLinks Links { get; set; } = null!;
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

public class PaginationLinks
{
    public string Self { get; set; } = string.Empty;
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}
