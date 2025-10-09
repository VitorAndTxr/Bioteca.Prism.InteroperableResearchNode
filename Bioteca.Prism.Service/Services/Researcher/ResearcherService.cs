using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Service.Interfaces.Researcher;

namespace Bioteca.Prism.Service.Services.Researcher;

/// <summary>
/// Service implementation for researcher operations
/// </summary>
public class ResearcherService : BaseService<Domain.Entities.Researcher.Researcher, Guid>, IResearcherService
{
    private readonly IResearcherRepository _researcherRepository;

    public ResearcherService(IResearcherRepository repository) : base(repository)
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
}
