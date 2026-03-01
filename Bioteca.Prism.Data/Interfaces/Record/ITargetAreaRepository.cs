using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Interfaces.Record;

/// <summary>
/// Repository interface for target area persistence operations
/// </summary>
public interface ITargetAreaRepository : IBaseRepository<TargetArea, Guid>
{
    /// <summary>
    /// Get target areas by record session ID
    /// </summary>
    Task<List<TargetArea>> GetByRecordSessionIdAsync(Guid recordSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get target areas by body structure code
    /// </summary>
    Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default);
}
