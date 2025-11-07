using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Interfaces.Snomed;

/// <summary>
/// Service interface for SNOMED CT body structure code operations
/// </summary>
public interface ISnomedBodyStructureService : IServiceBase<SnomedBodyStructure, string>
{
    /// <summary>
    /// Get active body structure codes
    /// </summary>
    Task<List<SnomedBodyStructureDTO>> GetActiveAsync();

    /// <summary>
    /// Get structures by body region
    /// </summary>
    Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode);

    /// <summary>
    /// Get sub-structures of a parent structure
    /// </summary>
    Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode);

    /// <summary>
    /// Get structures by type
    /// </summary>
    Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType);

    /// Get all body regions with pagination
    /// </summary>
    Task<List<SnomedBodyStructureDTO>> GetAllBodyStructuresPaginateAsync();


    Task<SnomedBodyStructure> AddAsync(AddSnomedBodyStructureDTO payload);
}
