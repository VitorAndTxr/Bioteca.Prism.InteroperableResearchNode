namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Response from POST /api/sync/preview.
/// Contains only aggregate metadata — no PII or clinical data is returned to the UI.
/// </summary>
public class SyncPreviewResponse
{
    public SyncManifestResponse Manifest { get; set; } = new();
    public DateTime? AutoResolvedSince { get; set; }
    public Guid RemoteNodeId { get; set; }
}
