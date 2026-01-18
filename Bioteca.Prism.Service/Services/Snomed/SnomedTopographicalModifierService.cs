using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT topographical modifier code operations
/// </summary>
public class SnomedTopographicalModifierService : BaseService<SnomedTopographicalModifier, string>, ISnomedTopographicalModifierService
{
    private readonly ISnomedTopographicalModifierRepository _snomedTopographicalModifierRepository;

    public SnomedTopographicalModifierService(ISnomedTopographicalModifierRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _snomedTopographicalModifierRepository = repository;
    }

    public async Task<List<SnomedTopographicalModifierDTO>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var result = await _snomedTopographicalModifierRepository.GetActiveAsync(cancellationToken);

        return result.Where(x => x.IsActive)
            .Select(modifier => new SnomedTopographicalModifierDTO
            {
                SnomedCode = modifier.Code,
                DisplayName = modifier.DisplayName,
                Category = modifier.Category,
                Description = modifier.Description
            }).ToList();
    }

    public async Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _snomedTopographicalModifierRepository.GetByCategoryAsync(category, cancellationToken);
    }

    public async Task<List<SnomedTopographicalModifierDTO>> GetAllTopographicalModifiersPaginateAsync()
    {
        var result = await _snomedTopographicalModifierRepository.GetPagedAsync();

        return result.Where(x => x.IsActive)
            .Select(modifier => new SnomedTopographicalModifierDTO
            {
                SnomedCode = modifier.Code,
                DisplayName = modifier.DisplayName,
                Category = modifier.Category,
                Description = modifier.Description
            }).ToList();
    }

    public async Task<SnomedTopographicalModifier> AddAsync(SnomedTopographicalModifierDTO payload)
    {
        ValidateAddTopographicalModifierPayload(payload);

        SnomedTopographicalModifier newModifier = new SnomedTopographicalModifier
        {
            Code = payload.SnomedCode,
            DisplayName = payload.DisplayName,
            Category = payload.Category,
            Description = payload.Description,
            IsActive = true
        };

        return await _snomedTopographicalModifierRepository.AddAsync(newModifier);
    }

    private void ValidateAddTopographicalModifierPayload(SnomedTopographicalModifierDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("Code is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.Category))
        {
            throw new ArgumentException("Category is required.");
        }

        if (_snomedTopographicalModifierRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("A topographical modifier with the same Code already exists.");
        }
    }

    public async Task<SnomedTopographicalModifierDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default)
    {
        var modifier = await _snomedTopographicalModifierRepository.GetByIdAsync(snomedCode);

        if (modifier == null)
        {
            return null;
        }

        return new SnomedTopographicalModifierDTO
        {
            SnomedCode = modifier.Code,
            DisplayName = modifier.DisplayName,
            Category = modifier.Category,
            Description = modifier.Description
        };
    }

    public async Task<SnomedTopographicalModifierDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedTopographicalModifierDTO payload, CancellationToken cancellationToken = default)
    {
        var existingModifier = await _snomedTopographicalModifierRepository.GetByIdAsync(snomedCode);

        if (existingModifier == null)
        {
            return null;
        }

        existingModifier.DisplayName = payload.DisplayName;
        existingModifier.Description = payload.Description;
        existingModifier.Category = payload.Category;

        await _snomedTopographicalModifierRepository.UpdateAsync(existingModifier);

        return new SnomedTopographicalModifierDTO
        {
            SnomedCode = existingModifier.Code,
            DisplayName = existingModifier.DisplayName,
            Category = existingModifier.Category,
            Description = existingModifier.Description
        };
    }
}
