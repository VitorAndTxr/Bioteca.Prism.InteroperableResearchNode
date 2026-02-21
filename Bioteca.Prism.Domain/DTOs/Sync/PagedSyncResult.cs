namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Paginated result wrapper for sync export endpoints.
/// Matches the PaginatedSyncResponse&lt;T&gt; type in @iris/domain.
/// </summary>
public class PagedSyncResult<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}
