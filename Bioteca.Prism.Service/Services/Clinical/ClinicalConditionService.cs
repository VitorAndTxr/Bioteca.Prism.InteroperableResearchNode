using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for clinical condition operations
/// </summary>
public class ClinicalConditionService : BaseService<ClinicalCondition, string>, IClinicalConditionService
{
    private readonly IBaseRepository<ClinicalCondition, string> _conditionRepository;

    public ClinicalConditionService(IBaseRepository<ClinicalCondition, string> repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _conditionRepository = repository;
    }

    public async Task<List<ClinicalCondition>> GetActiveConditionsAsync()
    {
        var allConditions = await _conditionRepository.GetAllAsync();
        return allConditions.Where(c => c.IsActive).ToList();
    }

    public async Task<List<ClinicalCondition>> SearchByNameAsync(string searchTerm)
    {
        var allConditions = await _conditionRepository.GetAllAsync();
        return allConditions
            .Where(c => c.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
