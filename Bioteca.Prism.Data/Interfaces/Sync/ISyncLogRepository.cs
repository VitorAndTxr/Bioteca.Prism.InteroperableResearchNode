using Bioteca.Prism.Domain.Entities.Sync;

namespace Bioteca.Prism.Data.Interfaces.Sync;

/// <summary>
/// Repository interface for sync log persistence operations
/// </summary>
public interface ISyncLogRepository
{
    /// <summary>
    /// Add a new sync log entry
    /// </summary>
    Task<SyncLog> AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing sync log entry
    /// </summary>
    Task<SyncLog> UpdateAsync(SyncLog syncLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated sync log entries for a remote node, ordered by StartedAt descending
    /// </summary>
    Task<(List<SyncLog> items, int totalCount)> GetByRemoteNodeIdAsync(
        Guid remoteNodeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent completed sync log for a remote node (used for watermark retrieval)
    /// </summary>
    Task<SyncLog?> GetLatestCompletedAsync(Guid remoteNodeId, CancellationToken cancellationToken = default);
}
