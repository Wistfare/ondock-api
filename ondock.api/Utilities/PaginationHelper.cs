using Microsoft.AspNetCore.WebUtilities;
using ondock.api.DTOs.Common;

namespace ondock.api.Utilities;

public static class PaginationHelper
{
    public static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> data,
        int page,
        int pageSize,
        int totalRecords,
        string baseUrl,
        Dictionary<string, string>? additionalParams = null)
    {
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        
        return new PagedResult<T>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            },
            Links = GeneratePaginationLinks(baseUrl, page, pageSize, totalPages, additionalParams)
        };
    }

    private static PaginationLinks GeneratePaginationLinks(
        string baseUrl,
        int currentPage,
        int pageSize,
        int totalPages,
        Dictionary<string, string>? additionalParams)
    {
        var links = new PaginationLinks
        {
            Self = BuildUrl(baseUrl, currentPage, pageSize, additionalParams),
            First = BuildUrl(baseUrl, 1, pageSize, additionalParams),
            Last = BuildUrl(baseUrl, totalPages, pageSize, additionalParams)
        };

        if (currentPage < totalPages)
        {
            links.Next = BuildUrl(baseUrl, currentPage + 1, pageSize, additionalParams);
        }

        if (currentPage > 1)
        {
            links.Previous = BuildUrl(baseUrl, currentPage - 1, pageSize, additionalParams);
        }

        return links;
    }

    private static string BuildUrl(
        string baseUrl,
        int page,
        int pageSize,
        Dictionary<string, string>? additionalParams)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "page", page.ToString() },
            { "pageSize", pageSize.ToString() }
        };

        if (additionalParams != null)
        {
            foreach (var param in additionalParams)
            {
                queryParams[param.Key] = param.Value;
            }
        }

        return QueryHelpers.AddQueryString(baseUrl, queryParams);
    }
}
