using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for allergy/intolerance operations
/// </summary>
public class AllergyIntoleranceService : BaseService<AllergyIntolerance, string>, IAllergyIntoleranceService
{
    private readonly IBaseRepository<AllergyIntolerance, string> _allergyRepository;

    public AllergyIntoleranceService(IBaseRepository<AllergyIntolerance, string> repository) : base(repository)
    {
        _allergyRepository = repository;
    }

    public async Task<List<AllergyIntolerance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var allAllergies = await _allergyRepository.GetAllAsync(cancellationToken);
        return allAllergies.Where(a => a.IsActive).ToList();
    }

    public async Task<List<AllergyIntolerance>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var allAllergies = await _allergyRepository.GetAllAsync(cancellationToken);
        return allAllergies
            .Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<AllergyIntolerance>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        var allAllergies = await _allergyRepository.GetAllAsync(cancellationToken);
        return allAllergies
            .Where(a => a.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
