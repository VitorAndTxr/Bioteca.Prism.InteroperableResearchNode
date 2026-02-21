namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Request body for POST /api/sync/manifest.
/// </summary>
public class SyncManifestRequest
{
    /// <summary>
    /// ISO 8601 timestamp. When provided, counts are filtered to records modified after this time.
    /// When null, counts include all records.
    /// </summary>
    public DateTime? Since { get; set; }
}
