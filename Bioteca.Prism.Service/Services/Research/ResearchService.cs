using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Service.Interfaces.Research;

namespace Bioteca.Prism.Service.Services.Research;

/// <summary>
/// Service implementation for research project operations
/// </summary>
public class ResearchService : BaseService<Domain.Entities.Research.Research, Guid>, IResearchService
{
    private readonly IResearchRepository _researchRepository;

    public ResearchService(IResearchRepository repository) : base(repository)
    {
        _researchRepository = repository;
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByStatusAsync(status, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetActiveResearchAsync(cancellationToken);
    }
}
