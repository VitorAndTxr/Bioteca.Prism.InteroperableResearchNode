using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for allergy/intolerance operations
/// </summary>
public class AllergyIntoleranceService : BaseService<AllergyIntolerance, string>, IAllergyIntoleranceService
{
    private readonly IAllergyIntoleranceRepository _allergyRepository;

    public AllergyIntoleranceService(IAllergyIntoleranceRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _allergyRepository = repository;
    }

    public async Task<List<AllergyIntolerance>> GetActiveAsync()
    {
        var allAllergies = await _allergyRepository.GetAllAsync();
        return allAllergies.Where(a => a.IsActive).ToList();
    }

    public async Task<List<AllergyIntolerance>> GetByCategoryAsync(string category)
    {
        var allAllergies = await _allergyRepository.GetAllAsync();
        return allAllergies
            .Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<AllergyIntolerance>> GetByTypeAsync(string type)
    {
        var allAllergies = await _allergyRepository.GetAllAsync();
        return allAllergies
            .Where(a => a.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<SnomedAllergyIntoleranceDTO>> GetActiveAllergyIntolerancesAsync()
    {
        var allAllergies = await _allergyRepository.GetAllAsync();
        return allAllergies.Where(a => a.IsActive)
            .Select(allergy => new SnomedAllergyIntoleranceDTO
            {
                SnomedCode = allergy.SnomedCode,
                Category = allergy.Category,
                SubstanceName = allergy.SubstanceName,
                Type = allergy.Type
            }).ToList();
    }

    public async Task<List<SnomedAllergyIntoleranceDTO>> GetAllAllergyIntolerancesPaginateAsync()
    {
        var result = await _allergyRepository.GetPagedAsync();

        return result.Where(x => x.IsActive)
            .Select(allergy => new SnomedAllergyIntoleranceDTO
            {
                SnomedCode = allergy.SnomedCode,
                Category = allergy.Category,
                SubstanceName = allergy.SubstanceName,
                Type = allergy.Type
            }).ToList();
    }

    public async Task<AllergyIntolerance> AddAsync(SnomedAllergyIntoleranceDTO payload)
    {
        ValidateAddAllergyIntolerancePayload(payload);

        AllergyIntolerance newAllergy = new AllergyIntolerance
        {
            SnomedCode = payload.SnomedCode,
            Category = payload.Category,
            SubstanceName = payload.SubstanceName,
            Type = payload.Type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _allergyRepository.AddAsync(newAllergy);
    }

    private void ValidateAddAllergyIntolerancePayload(SnomedAllergyIntoleranceDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("SnomedCode is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.SubstanceName))
        {
            throw new ArgumentException("SubstanceName is required.");
        }

        if (_allergyRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("An allergy/intolerance with the same SnomedCode already exists.");
        }
    }

    public async Task<SnomedAllergyIntoleranceDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default)
    {
        var allergy = await _allergyRepository.GetByIdAsync(snomedCode);

        if (allergy == null)
        {
            return null;
        }

        return new SnomedAllergyIntoleranceDTO
        {
            SnomedCode = allergy.SnomedCode,
            Category = allergy.Category,
            SubstanceName = allergy.SubstanceName,
            Type = allergy.Type
        };
    }

    public async Task<SnomedAllergyIntoleranceDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedAllergyIntoleranceDTO payload, CancellationToken cancellationToken = default)
    {
        var existingAllergy = await _allergyRepository.GetByIdAsync(snomedCode);

        if (existingAllergy == null)
        {
            return null;
        }

        existingAllergy.Category = payload.Category;
        existingAllergy.SubstanceName = payload.SubstanceName;
        existingAllergy.Type = payload.Type;
        existingAllergy.UpdatedAt = DateTime.UtcNow;

        await _allergyRepository.UpdateAsync(existingAllergy);

        return new SnomedAllergyIntoleranceDTO
        {
            SnomedCode = existingAllergy.SnomedCode,
            Category = existingAllergy.Category,
            SubstanceName = existingAllergy.SubstanceName,
            Type = existingAllergy.Type
        };
    }
}
