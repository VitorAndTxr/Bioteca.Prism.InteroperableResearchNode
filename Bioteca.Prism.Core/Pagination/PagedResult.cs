namespace Bioteca.Prism.Domain.DTOs.Paging;

/// <summary>
/// Generic wrapper for paginated API responses
/// </summary>
/// <typeparam name="T">Type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Collection of items for the current page
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of records across all pages
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}