using Bioteca.Prism.Domain.Entities.Application;

namespace Bioteca.Prism.Service.Services.Application;

/// <summary>
/// Service interface for application operations
/// </summary>
public interface IApplicationService : IService<Domain.Entities.Application.Application, Guid>
{
    /// <summary>
    /// Get applications by research ID
    /// </summary>
    Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);
}
