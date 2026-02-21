namespace Bioteca.Prism.Domain.Entities.Sync;

/// <summary>
/// Records a sync operation history entry per remote node.
/// LastSyncedAt serves as the watermark for incremental delta queries on subsequent syncs.
/// </summary>
public class SyncLog
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the remote research node this sync was pulled from
    /// </summary>
    public Guid RemoteNodeId { get; set; }

    /// <summary>
    /// Timestamp when the sync operation was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the sync operation completed (null if in_progress or failed before completion)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Sync status: in_progress, completed, failed, rolled_back
    /// </summary>
    public string Status { get; set; } = "in_progress";

    /// <summary>
    /// Watermark: the ManifestGeneratedAt from the remote node at the time of the last successful sync.
    /// Used as the ?since= parameter for the next incremental sync.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// JSON object with per-entity-type counts: {"snomed":42,"volunteers":10,"research":5,"sessions":8,"recordings":2}
    /// </summary>
    public string? EntitiesReceived { get; set; }

    /// <summary>
    /// Error message if the sync failed or was rolled back
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    public Node.ResearchNode RemoteNode { get; set; } = null!;
}
