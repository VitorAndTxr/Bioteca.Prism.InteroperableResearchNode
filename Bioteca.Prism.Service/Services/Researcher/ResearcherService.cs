using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Domain.DTOs.Researcher;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Service.Interfaces.Researcher;

namespace Bioteca.Prism.Service.Services.Researcher;

/// <summary>
/// Service implementation for researcher operations
/// </summary>
public class ResearcherService : BaseService<Domain.Entities.Researcher.Researcher, Guid>, IResearcherService
{
    private readonly IResearcherRepository _researcherRepository;

    public ResearcherService(IResearcherRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _researcherRepository = repository;
    }

    public async Task<List<ResearcherDTO>> GetByNodeIdAsync(Guid nodeId)
    {
        var researcher = await _researcherRepository.GetByNodeIdAsync(nodeId);

        return researcher.Select(r => new ResearcherDTO
        {
            ResearcherId = r.ResearcherId,
            Name = r.Name,
            Email = r.Email,
            Role = r.Role,
            Orcid = r.Orcid
        }).ToList();
    }

    public async Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution)
    {
        return await _researcherRepository.GetByInstitutionAsync(institution);
    }

    public async Task<Domain.Entities.Researcher.Researcher?> AddAsync(AddResearcherPayload payload)
    {
        ValidateAddResearcherPayload(payload);

        Domain.Entities.Researcher.Researcher researcher = new Domain.Entities.Researcher.Researcher
        {
            ResearcherId = Guid.NewGuid(),
            Name = payload.Name,
            Email = payload.Email,
            Orcid = payload.Orcid.Replace("-",""),
            Role = payload.Role,
            ResearchNodeId = payload.ResearchNodeId,
            Institution = payload.Institution,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return await _researcherRepository.AddAsync(researcher);
    }

    private void ValidateAddResearcherPayload(AddResearcherPayload payload)
    {
        if (string.IsNullOrEmpty(payload.Name))
        {
            throw new Exception("Invalid payload: You need to add a name");
        }

        if (string.IsNullOrEmpty(payload.Email))
        {
            throw new Exception("Invalid payload: You need to add a email");
        }

        if (string.IsNullOrEmpty(payload.Orcid))
        {
            throw new Exception("Invalid payload: You need to add a orcid");
        }

        if (string.IsNullOrEmpty(payload.Role))
        {
            throw new Exception("Invalid payload: You need to add a role");
        }

        if (_researcherRepository.GetByOrcidAsync(payload.Orcid).Result != null)
        {
            throw new Exception("Researcher already exists");
        }

        if (_researcherRepository.GetByEmailAsync(payload.Orcid).Result != null)
        {
            throw new Exception("Email already in use");
        }
    }

    public async Task<List<ResearcherDTO>> GetAllResearchersPaginateAsync()
    {
        var result = await _researcherRepository.GetPagedAsync();

        var mappedResult = result.Select(researcher => new ResearcherDTO
        {
            ResearcherId = researcher.ResearcherId,
            Name = researcher.Name,
            Email = researcher.Email,
            Role = researcher.Role,
            Orcid = researcher.Orcid
        }).ToList();

        return mappedResult;
    }

    public async Task<ResearcherDTO?> GetByResearcherIdAsync(Guid researcherId)
    {
        var researcher = await _researcherRepository.GetByIdAsync(researcherId);

        if (researcher == null)
        {
            return null;
        }

        return new ResearcherDTO
        {
            ResearcherId = researcher.ResearcherId,
            Name = researcher.Name,
            Email = researcher.Email,
            Role = researcher.Role,
            Orcid = researcher.Orcid
        };
    }

    public async Task<ResearcherDTO?> UpdateResearcherAsync(Guid id, UpdateResearcherPayload payload)
    {
        var researcher = await _researcherRepository.GetByIdAsync(id);

        if (researcher == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(payload.Name))
        {
            researcher.Name = payload.Name;
        }

        if (!string.IsNullOrEmpty(payload.Email))
        {
            var existingByEmail = await _researcherRepository.GetByEmailAsync(payload.Email);
            if (existingByEmail != null && existingByEmail.ResearcherId != id)
            {
                throw new Exception("Email already in use by another researcher");
            }
            researcher.Email = payload.Email;
        }

        if (!string.IsNullOrEmpty(payload.Institution))
        {
            researcher.Institution = payload.Institution;
        }

        if (!string.IsNullOrEmpty(payload.Role))
        {
            researcher.Role = payload.Role;
        }

        if (!string.IsNullOrEmpty(payload.Orcid))
        {
            var normalizedOrcid = payload.Orcid.Replace("-", "");
            var existingByOrcid = await _researcherRepository.GetByOrcidAsync(normalizedOrcid);
            if (existingByOrcid != null && existingByOrcid.ResearcherId != id)
            {
                throw new Exception("ORCID already in use by another researcher");
            }
            researcher.Orcid = normalizedOrcid;
        }

        researcher.UpdatedAt = DateTime.UtcNow;

        await _researcherRepository.UpdateAsync(researcher);

        return new ResearcherDTO
        {
            ResearcherId = researcher.ResearcherId,
            Name = researcher.Name,
            Email = researcher.Email,
            Role = researcher.Role,
            Orcid = researcher.Orcid
        };
    }
}
