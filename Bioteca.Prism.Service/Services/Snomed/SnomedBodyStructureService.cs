using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT body structure code operations
/// </summary>
public class SnomedBodyStructureService : BaseService<SnomedBodyStructure, string>, ISnomedBodyStructureService
{
    private readonly ISnomedBodyStructureRepository _snomedBodyStructureRepository;

    public SnomedBodyStructureService(ISnomedBodyStructureRepository repository, IApiContext apiContext) : base(repository, apiContext)
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
