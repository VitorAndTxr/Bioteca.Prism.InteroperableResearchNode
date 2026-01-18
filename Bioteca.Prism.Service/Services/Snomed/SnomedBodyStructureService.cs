using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT body structure code operations
/// </summary>
public class SnomedBodyStructureService : BaseService<SnomedBodyStructure, string>, ISnomedBodyStructureService
{
    private readonly ISnomedBodyStructureRepository _snomedBodyStructureRepository;
    private readonly ISnomedBodyRegionRepository _snomedBodyRegionRepository;


    public SnomedBodyStructureService(
        ISnomedBodyStructureRepository repository, 
        IApiContext apiContext,
        ISnomedBodyRegionRepository snomedBodyRegionRepository) : base(repository, apiContext)
    {
        _snomedBodyStructureRepository = repository;
        _snomedBodyRegionRepository = snomedBodyRegionRepository;
    }

    public async Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode)
    {
        return await _snomedBodyStructureRepository.GetByBodyRegionAsync(bodyRegionCode);
    }

    public async Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode)
    {
        return await _snomedBodyStructureRepository.GetSubStructuresAsync(parentStructureCode);
    }

    public async Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType)
    {
        return await _snomedBodyStructureRepository.GetByStructureTypeAsync(structureType);
    }

    public async Task<List<SnomedBodyStructureDTO>> GetActiveAsync()
    {
        var result = await _snomedBodyStructureRepository.GetActiveAsync();

        return result.Where(x => x.IsActive).Select(MapToDto).ToList();
    }

    public async Task<List<SnomedBodyStructureDTO>> GetAllBodyStructuresPaginateAsync()
    {
        var result = await _snomedBodyStructureRepository.GetPagedAsync();

        return result.Where(x => x.IsActive).Select(MapToDto).ToList();
    }

    public async Task<SnomedBodyStructure> AddAsync(AddSnomedBodyStructureDTO payload)
    {
        ValidateAddSnomedBodyStructurePayload(payload);

        SnomedBodyStructure newBodyStructure = new SnomedBodyStructure
        {
            SnomedCode = payload.SnomedCode,
            DisplayName = payload.DisplayName,
            Description = payload.Description,
            StructureType = payload.Type,
            BodyRegionCode = payload.BodyRegionCode,
            // Convert empty strings to null for optional foreign key
            ParentStructureCode = string.IsNullOrWhiteSpace(payload.ParentStructureCode)
                ? null
                : payload.ParentStructureCode,
            IsActive = true
        };

        return await _snomedBodyStructureRepository.AddAsync(newBodyStructure);
    }

    public void ValidateAddSnomedBodyStructurePayload(AddSnomedBodyStructureDTO payload)
    {
        // Example validation logic
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("SnomedCode is required.");
        }
        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if(string.IsNullOrWhiteSpace(payload.BodyRegionCode))
        {
            throw new ArgumentException("BodyRegionCode is required.");
        }

        if(string.IsNullOrWhiteSpace(payload.Type))
        {
            throw new ArgumentException("StructureType is required.");
        }

        if(_snomedBodyStructureRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("A body structure with the same SnomedCode already exists.");
        }

        if(payload.BodyRegionCode != null && _snomedBodyRegionRepository.GetByIdAsync(payload.BodyRegionCode).Result == null)
        {
            throw new ArgumentException("The specified BodyRegionCode does not exist.");
        }

        // Validate ParentStructureCode exists if provided (null/empty is allowed for root structures)
        if(!string.IsNullOrWhiteSpace(payload.ParentStructureCode) &&
           _snomedBodyStructureRepository.GetByIdAsync(payload.ParentStructureCode).Result == null)
        {
            throw new ArgumentException($"The specified ParentStructureCode '{payload.ParentStructureCode}' does not exist.");
        }
    }

    public async Task<SnomedBodyStructureDTO?> GetBySnomedCodeAsync(string snomedCode)
    {
        var bodyStructure = await _snomedBodyStructureRepository.GetBySnomedCodeWithNavigationAsync(snomedCode);

        if (bodyStructure == null)
        {
            return null;
        }

        return MapToDto(bodyStructure);
    }

    public async Task<SnomedBodyStructureDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedBodyStructureDTO payload)
    {
        var bodyStructure = await _snomedBodyStructureRepository.GetByIdAsync(snomedCode);

        if (bodyStructure == null)
        {
            return null;
        }

        ValidateUpdateSnomedBodyStructurePayload(payload);

        bodyStructure.DisplayName = payload.DisplayName;
        bodyStructure.Description = payload.Description;
        bodyStructure.StructureType = payload.Type;
        if (!string.IsNullOrWhiteSpace(payload.BodyRegionCode))
        {
            bodyStructure.BodyRegionCode = payload.BodyRegionCode;
        }
        bodyStructure.UpdatedAt = DateTime.UtcNow;

        await _snomedBodyStructureRepository.UpdateAsync(bodyStructure);

        // Fetch updated entity with navigation properties
        var updatedBodyStructure = await _snomedBodyStructureRepository.GetBySnomedCodeWithNavigationAsync(snomedCode);

        return MapToDto(updatedBodyStructure!);
    }

    private void ValidateUpdateSnomedBodyStructurePayload(UpdateSnomedBodyStructureDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.Type))
        {
            throw new ArgumentException("Type is required.");
        }

        if (!string.IsNullOrWhiteSpace(payload.BodyRegionCode) &&
            _snomedBodyRegionRepository.GetByIdAsync(payload.BodyRegionCode).Result == null)
        {
            throw new ArgumentException($"Body region with code '{payload.BodyRegionCode}' does not exist.");
        }
    }

    /// <summary>
    /// Maps a SnomedBodyStructure entity to its DTO representation with nested objects.
    /// Includes up to 2 levels of depth for nested navigation properties.
    /// </summary>
    private static SnomedBodyStructureDTO MapToDto(SnomedBodyStructure entity)
    {
        return new SnomedBodyStructureDTO
        {
            SnomedCode = entity.SnomedCode,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Type = entity.StructureType,
            BodyRegion = entity.BodyRegion != null ? MapBodyRegionToDto(entity.BodyRegion) : null!,
            ParentStructure = entity.ParentStructure != null ? new SnomedBodyStructureDTO
            {
                SnomedCode = entity.ParentStructure.SnomedCode,
                DisplayName = entity.ParentStructure.DisplayName,
                Description = entity.ParentStructure.Description,
                Type = entity.ParentStructure.StructureType,
                BodyRegion = entity.ParentStructure.BodyRegion != null
                    ? MapBodyRegionToDto(entity.ParentStructure.BodyRegion)
                    : null!,
                // Stop recursion at 2nd level to avoid infinite nesting
                ParentStructure = null
            } : null
        };
    }

    /// <summary>
    /// Maps a SnomedBodyRegion entity to its DTO representation.
    /// </summary>
    private static SnomedBodyRegionDTO MapBodyRegionToDto(SnomedBodyRegion entity)
    {
        return new SnomedBodyRegionDTO
        {
            SnomedCode = entity.SnomedCode,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            ParentRegion = entity.ParentRegion != null ? new SnomedBodyRegionDTO
            {
                SnomedCode = entity.ParentRegion.SnomedCode,
                DisplayName = entity.ParentRegion.DisplayName,
                Description = entity.ParentRegion.Description,
                // Stop recursion at 2nd level
                ParentRegion = null
            } : null
        };
    }
}
