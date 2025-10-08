using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service interface for SNOMED CT body structure code operations
/// </summary>
public interface ISnomedBodyStructureService : IService<SnomedBodyStructure, string>
{
    /// <summary>
    /// Get active body structure codes
    /// </summary>
    Task<List<SnomedBodyStructure>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get structures by body region
    /// </summary>
    Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sub-structures of a parent structure
    /// </summary>
    Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get structures by type
    /// </summary>
    Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType, CancellationToken cancellationToken = default);
}
