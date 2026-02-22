namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Request body for POST /api/sync/preview and POST /api/sync/pull.
/// </summary>
public class SyncPullRequest
{
    /// <summary>
    /// ID of the remote research node to pull data from.
    /// Must match a node in the local registry with Status = Authorized.
    /// </summary>
    public Guid RemoteNodeId { get; set; }

    /// <summary>
    /// Optional watermark for incremental sync. When null, the service
    /// auto-resolves from the last successful SyncLog entry.
    /// </summary>
    public DateTime? Since { get; set; }
}
