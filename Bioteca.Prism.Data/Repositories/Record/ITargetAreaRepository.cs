using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository interface for target area persistence operations
/// </summary>
public interface ITargetAreaRepository : IRepository<TargetArea, Guid>
{
    /// <summary>
    /// Get target areas by record channel ID
    /// </summary>
    Task<List<TargetArea>> GetByRecordChannelIdAsync(Guid recordChannelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get target areas by body structure code
    /// </summary>
    Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default);
}
