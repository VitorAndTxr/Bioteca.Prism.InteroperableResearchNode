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

        return result.Where(x => x.IsActive).Select(bodyRegion => new SnomedBodyStructureDTO
        {
            SnomedCode = bodyRegion.SnomedCode,
            DisplayName = bodyRegion.DisplayName,
            Description = bodyRegion.Description
        }).ToList();
    }

    public async Task<List<SnomedBodyStructureDTO>> GetAllBodyStructuresPaginateAsync()
    {
        var result = await _snomedBodyStructureRepository.GetPagedAsync();

        var mappedResult = result.Where(x => x.IsActive).Select(bodyStructure => new SnomedBodyStructureDTO
        {
            SnomedCode = bodyStructure.SnomedCode,
            DisplayName = bodyStructure.DisplayName,
            Description = bodyStructure.Description,
            Type = bodyStructure.StructureType,
            ParentStructure = bodyStructure.ParentStructure != null ? new SnomedBodyStructureDTO
            {
                SnomedCode = bodyStructure.ParentStructure.SnomedCode,
                DisplayName = bodyStructure.ParentStructure.DisplayName,
                Description = bodyStructure.ParentStructure.Description
            } : null,
            BodyRegion =  new SnomedBodyRegionDTO
            {
                SnomedCode = bodyStructure.BodyRegion.SnomedCode,
                DisplayName = bodyStructure.BodyRegion.DisplayName,
                Description = bodyStructure.BodyRegion.Description
            } 
        }).ToList();

        return mappedResult;
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
            ParentStructureCode = payload.ParentStructureCode,
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
    }
}
