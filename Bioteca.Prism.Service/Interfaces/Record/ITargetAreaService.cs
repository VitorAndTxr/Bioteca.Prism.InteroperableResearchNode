using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Service.Interfaces.Record;

/// <summary>
/// Service interface for target area operations
/// </summary>
public interface ITargetAreaService : IServiceBase<TargetArea, Guid>
{
    /// <summary>
    /// Get target areas by record channel ID (with SNOMED navigation properties)
    /// </summary>
    Task<List<TargetArea>> GetByRecordChannelIdAsync(Guid recordChannelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get target areas by body structure code
    /// </summary>
    Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default);
}
