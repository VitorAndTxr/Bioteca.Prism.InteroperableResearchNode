using Bioteca.Prism.Domain.DTOs.Sync;

namespace Bioteca.Prism.Service.Interfaces.Sync;

public interface ISyncPullService
{
    /// <summary>
    /// Performs the 4-phase handshake with the remote node and fetches the manifest
    /// without importing any data. Returns a preview for user confirmation.
    /// </summary>
    Task<SyncPreviewResponse> PreviewAsync(
        Guid remoteNodeId, DateTime? since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the full sync: handshake → manifest → fetch all entities → import.
    /// Returns the import result. Channel is closed on completion or failure.
    /// </summary>
    Task<SyncResultDTO> PullAsync(
        Guid remoteNodeId, DateTime? since,
        CancellationToken cancellationToken = default);
}
