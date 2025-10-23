using Bioteca.Prism.Core.Interfaces;
namespace Bioteca.Prism.Service.Interfaces.Application;

/// <summary>
/// Service interface for application operations
/// </summary>
public interface IApplicationService : IServiceBase<Domain.Entities.Application.Application, Guid>
{
    // TODO: Restore when ResearchApplication join table is implemented
    // /// <summary>
    // /// Get applications by research ID
    // /// </summary>
    // Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);
}
