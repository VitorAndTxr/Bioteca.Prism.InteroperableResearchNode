using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service interface for target area operations
/// </summary>
public interface ITargetAreaService : IService<TargetArea, Guid>
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
