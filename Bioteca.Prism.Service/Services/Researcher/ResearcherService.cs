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

    public Task<Domain.Entities.Researcher.Researcher?> AddAsync(AddUserPayload payload)
    {
        throw new NotImplementedException();
    }

    public async Task<List<UserDTO>> GetAllResearchersPaginateAsync()
    {
        //var result = await _researcherRepository.GetPagedAsync();

        //var mappedResult = result.Select(user => new UserDTO
        //{
        //    Id = user.Id,
        //    Login = user.Login,
        //    Role = user.Role,
        //    CreatedAt = user.CreatedAt,
        //    UpdatedAt = user.UpdatedAt,
        //    Researcher = user.Researcher != null ? new ResearcherInfoDto()
        //    {
        //        Name = user.Researcher.Name,
        //        Email = user.Researcher.Email,
        //        Role = user.Researcher.Role,
        //        Orcid = user.Researcher.Orcid
        //    } : null
        //}).ToList();

        //return mappedResult;
        throw new NotImplementedException();

    }
}
