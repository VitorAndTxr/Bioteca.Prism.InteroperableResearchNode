using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.DTOs.Snomed;
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

    public async Task<List<SnomedBodyRegionDTO>> GetActiveAsync()
    {
        var result = await _snomedBodyRegionRepository.GetActiveAsync();

        return result.Where(x => x.IsActive).Select(bodyRegion => new SnomedBodyRegionDTO
        {
            SnomedCode = bodyRegion.SnomedCode,
            DisplayName = bodyRegion.DisplayName,
            Description = bodyRegion.Description
        }).ToList();
    }

    public async Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode)
    {
        return await _snomedBodyRegionRepository.GetSubRegionsAsync(parentRegionCode);
    }

    public async Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync()
    {
        return await _snomedBodyRegionRepository.GetTopLevelRegionsAsync();
    }

    public async Task<List<SnomedBodyRegionDTO>>    GetAllBodyRegionsPaginateAsync()
    {
        var result = await _snomedBodyRegionRepository.GetPagedAsync();

        var mappedResult = result.Where(x => x.IsActive).Select(bodyRegion => new SnomedBodyRegionDTO
        {
            SnomedCode = bodyRegion.SnomedCode,
            DisplayName = bodyRegion.DisplayName,
            Description = bodyRegion.Description,
            ParentRegion = bodyRegion.ParentRegion != null ? new SnomedBodyRegionDTO
            {
                SnomedCode = bodyRegion.ParentRegion.SnomedCode,
                DisplayName = bodyRegion.ParentRegion.DisplayName,
                Description = bodyRegion.ParentRegion.Description
            } : null
        }).ToList();

        return mappedResult;
    }

    public async Task<SnomedBodyRegion> AddAsync(AddSnomedBodyRegionDTO payload)
    {
        ValidadeAddSnomedBodyRegionPayload(payload);

        SnomedBodyRegion snomedBodyRegion = new SnomedBodyRegion
        {
            SnomedCode = payload.SnomedCode,
            DisplayName = payload.DisplayName,
            Description = payload.Description,
            ParentRegionCode = payload.ParentRegionCode,
            IsActive = true
        };

        return await _snomedBodyRegionRepository.AddAsync(snomedBodyRegion);
    }

    private void ValidadeAddSnomedBodyRegionPayload(AddSnomedBodyRegionDTO payload)
    {
        if (string.IsNullOrEmpty(payload.SnomedCode) || string.IsNullOrEmpty(payload.DisplayName))
        {
            throw new Exception("Invalid payload");
        }
        if (_snomedBodyRegionRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new Exception("Body region with the same SNOMED code already exists");
        }

        if (payload.ParentRegionCode!=null&& _snomedBodyRegionRepository.GetByIdAsync(payload.ParentRegionCode).Result == null)
        {
            throw new Exception("Parent Body region code doesn't exists");
        }
    }

    public async Task<SnomedBodyRegionDTO?> GetBySnomedCodeAsync(string snomedCode)
    {
        var bodyRegion = await _snomedBodyRegionRepository.GetBySnomedCodeWithNavigationAsync(snomedCode);

        if (bodyRegion == null)
        {
            return null;
        }

        return new SnomedBodyRegionDTO
        {
            SnomedCode = bodyRegion.SnomedCode,
            DisplayName = bodyRegion.DisplayName,
            Description = bodyRegion.Description,
            ParentRegion = bodyRegion.ParentRegion != null ? new SnomedBodyRegionDTO
            {
                SnomedCode = bodyRegion.ParentRegion.SnomedCode,
                DisplayName = bodyRegion.ParentRegion.DisplayName,
                Description = bodyRegion.ParentRegion.Description
            } : null
        };
    }

    public async Task<SnomedBodyRegionDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedBodyRegionDTO payload)
    {
        var bodyRegion = await _snomedBodyRegionRepository.GetByIdAsync(snomedCode);

        if (bodyRegion == null)
        {
            return null;
        }

        ValidateUpdateSnomedBodyRegionPayload(payload);

        bodyRegion.DisplayName = payload.DisplayName;
        bodyRegion.Description = payload.Description;
        bodyRegion.ParentRegionCode = payload.ParentRegionCode;
        bodyRegion.UpdatedAt = DateTime.UtcNow;

        await _snomedBodyRegionRepository.UpdateAsync(bodyRegion);

        // Fetch updated entity with navigation properties
        var updatedBodyRegion = await _snomedBodyRegionRepository.GetBySnomedCodeWithNavigationAsync(snomedCode);

        return new SnomedBodyRegionDTO
        {
            SnomedCode = updatedBodyRegion!.SnomedCode,
            DisplayName = updatedBodyRegion.DisplayName,
            Description = updatedBodyRegion.Description,
            ParentRegion = updatedBodyRegion.ParentRegion != null ? new SnomedBodyRegionDTO
            {
                SnomedCode = updatedBodyRegion.ParentRegion.SnomedCode,
                DisplayName = updatedBodyRegion.ParentRegion.DisplayName,
                Description = updatedBodyRegion.ParentRegion.Description
            } : null
        };
    }

    private void ValidateUpdateSnomedBodyRegionPayload(UpdateSnomedBodyRegionDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if (!string.IsNullOrWhiteSpace(payload.ParentRegionCode) &&
            _snomedBodyRegionRepository.GetByIdAsync(payload.ParentRegionCode).Result == null)
        {
            throw new ArgumentException($"Parent body region with code '{payload.ParentRegionCode}' does not exist.");
        }
    }
}
