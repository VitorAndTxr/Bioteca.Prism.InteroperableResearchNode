using Bioteca.Prism.Data.Repositories.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT laterality code operations
/// </summary>
public class SnomedLateralityService : Service<SnomedLaterality, string>, ISnomedLateralityService
{
    private readonly ISnomedLateralityRepository _snomedLateralityRepository;

    public SnomedLateralityService(ISnomedLateralityRepository repository) : base(repository)
    {
        _snomedLateralityRepository = repository;
    }

    public async Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _snomedLateralityRepository.GetActiveAsync(cancellationToken);
    }
}
