using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT body region code operations
/// </summary>
public class SnomedBodyRegionService : BaseService<SnomedBodyRegion, string>, ISnomedBodyRegionService
{
    private readonly ISnomedBodyRegionRepository _snomedBodyRegionRepository;

    public SnomedBodyRegionService(ISnomedBodyRegionRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _snomedBodyRegionRepository = repository;
    }

    public async Task<List<SnomedBodyRegion>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _snomedBodyRegionRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode, CancellationToken cancellationToken = default)
    {
        return await _snomedBodyRegionRepository.GetSubRegionsAsync(parentRegionCode, cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await _snomedBodyRegionRepository.GetTopLevelRegionsAsync(cancellationToken);
    }
}
