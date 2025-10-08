using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service interface for SNOMED CT body region code operations
/// </summary>
public interface ISnomedBodyRegionService : IService<SnomedBodyRegion, string>
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
