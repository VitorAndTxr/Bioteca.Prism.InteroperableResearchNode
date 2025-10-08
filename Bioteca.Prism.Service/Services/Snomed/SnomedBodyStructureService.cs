using Bioteca.Prism.Data.Repositories.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT body structure code operations
/// </summary>
public class SnomedBodyStructureService : Service<SnomedBodyStructure, string>, ISnomedBodyStructureService
{
    private readonly ISnomedBodyStructureRepository _snomedBodyStructureRepository;

    public SnomedBodyStructureService(ISnomedBodyStructureRepository repository) : base(repository)
    {
        _snomedBodyStructureRepository = repository;
    }

    public async Task<List<SnomedBodyStructure>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _snomedBodyStructureRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode, CancellationToken cancellationToken = default)
    {
        return await _snomedBodyStructureRepository.GetByBodyRegionAsync(bodyRegionCode, cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode, CancellationToken cancellationToken = default)
    {
        return await _snomedBodyStructureRepository.GetSubStructuresAsync(parentStructureCode, cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType, CancellationToken cancellationToken = default)
    {
        return await _snomedBodyStructureRepository.GetByStructureTypeAsync(structureType, cancellationToken);
    }
}
