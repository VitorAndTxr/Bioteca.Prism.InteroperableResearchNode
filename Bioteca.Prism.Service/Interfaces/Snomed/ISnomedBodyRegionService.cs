using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
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
    Task<List<SnomedBodyRegionDTO>> GetActiveAsync();

    /// <summary>
    /// Get sub-regions of a parent region
    /// </summary>
    Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode);

    /// <summary>
    /// Get top-level regions (no parent)
    /// </summary>
    Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync();

    /// <summary>
    /// Get all body regions with pagination
    /// </summary>
    Task<List<SnomedBodyRegionDTO>> GetAllBodyRegionsPaginateAsync();


    Task<SnomedBodyRegion> AddAsync(AddSnomedBodyRegionDTO payload);
}
