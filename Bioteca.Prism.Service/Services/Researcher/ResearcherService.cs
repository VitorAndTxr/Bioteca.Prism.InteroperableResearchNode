using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Domain.DTOs.User;
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

    public async Task<List<Domain.Entities.Researcher.Researcher>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _researcherRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution, CancellationToken cancellationToken = default)
    {
        return await _researcherRepository.GetByInstitutionAsync(institution, cancellationToken);
    }

    public Task<Domain.Entities.Researcher.Researcher?> AddAsync(AddResearcherPayload payload)
    {
        ValidateAddResearcherPayload(payload);

        Domain.Entities.Researcher.Researcher researcher = new Domain.Entities.Researcher.Researcher
        {
            ResearcherId = Guid.NewGuid(),
            Name = payload.Name,
            Email = payload.Email,
            Orcid = payload.Orcid,
            Role = payload.Role,
            ResearchNodeId = payload.ResearchNodeId,
            Institution = payload.Institution,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        throw new NotImplementedException();
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

        if (_researcherRepository.GetByOrcidAsync(payload.Orcid).Result == null)
        {
            throw new Exception("Researcher already exists");
        }

        if (_researcherRepository.GetByEmailAsync(payload.Orcid).Result == null)
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
}
