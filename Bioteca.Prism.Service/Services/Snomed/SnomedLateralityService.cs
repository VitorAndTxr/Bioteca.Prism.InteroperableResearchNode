using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT laterality code operations
/// </summary>
public class SnomedLateralityService : BaseService<SnomedLaterality, string>, ISnomedLateralityService
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
