using Bioteca.Prism.Data.Repositories.Application;

namespace Bioteca.Prism.Service.Services.Application;

/// <summary>
/// Service implementation for application operations
/// </summary>
public class ApplicationService : Service<Domain.Entities.Application.Application, Guid>, IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;

    public ApplicationService(IApplicationRepository repository) : base(repository)
    {
        _applicationRepository = repository;
    }

    public async Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _applicationRepository.GetByResearchIdAsync(researchId, cancellationToken);
    }
}
