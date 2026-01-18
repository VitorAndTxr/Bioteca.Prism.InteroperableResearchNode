using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for clinical condition operations
/// </summary>
public class ClinicalConditionService : BaseService<ClinicalCondition, string>, IClinicalConditionService
{
    private readonly IClinicalConditionRepository _conditionRepository;

    public ClinicalConditionService(IClinicalConditionRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _conditionRepository = repository;
    }

    public async Task<List<SnomedClinicalConditionDTO>> GetActiveConditionsAsync()
    {
        var allConditions = await _conditionRepository.GetAllAsync();
        return allConditions.Where(c => c.IsActive)
            .Select(condition => new SnomedClinicalConditionDTO
            {
                SnomedCode = condition.SnomedCode,
                DisplayName = condition.DisplayName,
                Description = condition.Description
            }).ToList();
    }

    public async Task<List<ClinicalCondition>> SearchByNameAsync(string searchTerm)
    {
        var allConditions = await _conditionRepository.GetAllAsync();
        return allConditions
            .Where(c => c.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<SnomedClinicalConditionDTO>> GetAllClinicalConditionsPaginateAsync()
    {
        var result = await _conditionRepository.GetPagedAsync();

        return result.Where(x => x.IsActive)
            .Select(condition => new SnomedClinicalConditionDTO
            {
                SnomedCode = condition.SnomedCode,
                DisplayName = condition.DisplayName,
                Description = condition.Description
            }).ToList();
    }

    public async Task<ClinicalCondition> AddAsync(SnomedClinicalConditionDTO payload)
    {
        ValidateAddClinicalConditionPayload(payload);

        ClinicalCondition newCondition = new ClinicalCondition
        {
            SnomedCode = payload.SnomedCode,
            DisplayName = payload.DisplayName,
            Description = payload.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _conditionRepository.AddAsync(newCondition);
    }

    private void ValidateAddClinicalConditionPayload(SnomedClinicalConditionDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("SnomedCode is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if (_conditionRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("A clinical condition with the same SnomedCode already exists.");
        }
    }

    public async Task<SnomedClinicalConditionDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default)
    {
        var condition = await _conditionRepository.GetByIdAsync(snomedCode);

        if (condition == null)
        {
            return null;
        }

        return new SnomedClinicalConditionDTO
        {
            SnomedCode = condition.SnomedCode,
            DisplayName = condition.DisplayName,
            Description = condition.Description
        };
    }

    public async Task<SnomedClinicalConditionDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedClinicalConditionDTO payload, CancellationToken cancellationToken = default)
    {
        var existingCondition = await _conditionRepository.GetByIdAsync(snomedCode);

        if (existingCondition == null)
        {
            return null;
        }

        existingCondition.DisplayName = payload.DisplayName;
        existingCondition.Description = payload.Description;
        existingCondition.UpdatedAt = DateTime.UtcNow;

        await _conditionRepository.UpdateAsync(existingCondition);

        return new SnomedClinicalConditionDTO
        {
            SnomedCode = existingCondition.SnomedCode,
            DisplayName = existingCondition.DisplayName,
            Description = existingCondition.Description
        };
    }
}
