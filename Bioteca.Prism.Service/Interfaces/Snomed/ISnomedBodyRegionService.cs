using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Interfaces.Snomed;

/// <summary>
/// Service interface for SNOMED CT body region code operations
/// </summary>
public interface ISnomedBodyRegionService : IServiceBase<SnomedBodyRegion, string>
{
    /// <summary>
    /// Get active body region codes
    /// </summary>
    Task<List<SnomedBodyRegion>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sub-regions of a parent region
    /// </summary>
    Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top-level regions (no parent)
    /// </summary>
    Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync(CancellationToken cancellationToken = default);
}
