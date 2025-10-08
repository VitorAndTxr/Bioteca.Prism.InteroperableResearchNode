using Bioteca.Prism.Data.Repositories.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT body region code operations
/// </summary>
public class SnomedBodyRegionService : Service<SnomedBodyRegion, string>, ISnomedBodyRegionService
{
    private readonly ISnomedBodyRegionRepository _snomedBodyRegionRepository;

    public SnomedBodyRegionService(ISnomedBodyRegionRepository repository) : base(repository)
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
