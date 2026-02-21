using Bioteca.Prism.Domain.DTOs.Sync;

namespace Bioteca.Prism.Service.Interfaces.Sync;

/// <summary>
/// Service for importing entities from a remote node into this node's database.
/// All upserts happen within a single EF Core transaction to guarantee atomicity.
/// </summary>
public interface ISyncImportService
{
    /// <summary>
    /// Import all entities from the payload in dependency order within a single transaction.
    /// On any failure, rolls back the entire transaction and logs the error to SyncLog.
    /// </summary>
    /// <param name="payload">Collected sync payload from the remote node</param>
    /// <param name="remoteNodeId">ID of the remote node being synced from</param>
    Task<SyncResultDTO> ImportAsync(SyncImportPayload payload, Guid remoteNodeId);
}
