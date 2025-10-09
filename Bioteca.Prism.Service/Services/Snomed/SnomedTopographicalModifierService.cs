using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT topographical modifier code operations
/// </summary>
public class SnomedTopographicalModifierService : BaseService<SnomedTopographicalModifier, string>, ISnomedTopographicalModifierService
{
    private readonly ISnomedTopographicalModifierRepository _snomedTopographicalModifierRepository;

    public SnomedTopographicalModifierService(ISnomedTopographicalModifierRepository repository) : base(repository)
    {
        _snomedTopographicalModifierRepository = repository;
    }

    public async Task<List<SnomedTopographicalModifier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _snomedTopographicalModifierRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _snomedTopographicalModifierRepository.GetByCategoryAsync(category, cancellationToken);
    }
}
