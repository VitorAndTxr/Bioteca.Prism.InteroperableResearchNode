using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Data.Interfaces.Snomed;

/// <summary>
/// Repository interface for SNOMED CT laterality codes
/// </summary>
public interface ISnomedLateralityRepository : IBaseRepository<SnomedLaterality, string>
{
    /// <summary>
    /// Get active laterality codes
    /// </summary>
    Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for SNOMED CT topographical modifier codes
/// </summary>
public interface ISnomedTopographicalModifierRepository : IBaseRepository<SnomedTopographicalModifier, string>
{
    /// <summary>
    /// Get active topographical modifier codes
    /// </summary>
    Task<List<SnomedTopographicalModifier>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get modifiers by category
    /// </summary>
    Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for SNOMED CT body region codes
/// </summary>
public interface ISnomedBodyRegionRepository : IBaseRepository<SnomedBodyRegion, string>
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

/// <summary>
/// Repository interface for SNOMED CT body structure codes
/// </summary>
public interface ISnomedBodyStructureRepository : IBaseRepository<SnomedBodyStructure, string>
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
