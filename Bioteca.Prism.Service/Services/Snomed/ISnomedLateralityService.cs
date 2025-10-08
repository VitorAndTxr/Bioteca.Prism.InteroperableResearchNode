using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service interface for SNOMED CT laterality code operations
/// </summary>
public interface ISnomedLateralityService : IService<SnomedLaterality, string>
{
    /// <summary>
    /// Get active laterality codes
    /// </summary>
    Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default);
}
