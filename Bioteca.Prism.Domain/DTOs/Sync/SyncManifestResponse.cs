namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Response from POST /api/sync/manifest.
/// Provides entity counts and latest update timestamps for the requesting node
/// to estimate sync scope before proceeding.
/// </summary>
public class SyncManifestResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public SyncEntitySummaryDto Snomed { get; set; } = new();
    public SyncEntitySummaryDto Volunteers { get; set; } = new();
    public SyncEntitySummaryDto Research { get; set; } = new();
    public SyncEntitySummaryDto Sessions { get; set; } = new();
    public SyncRecordingSummaryDto Recordings { get; set; } = new();
}

public class SyncEntitySummaryDto
{
    public int Count { get; set; }
    public DateTime? LatestUpdate { get; set; }
}

public class SyncRecordingSummaryDto
{
    public int Count { get; set; }
    public long TotalSizeBytes { get; set; }
}
