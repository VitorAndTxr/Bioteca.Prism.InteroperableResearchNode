using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Application;

/// <summary>
/// Repository interface for application persistence operations
/// </summary>
public interface IApplicationRepository : IBaseRepository<Domain.Entities.Application.Application, Guid>
{
    // TODO: Restore when ResearchApplication join table is implemented
    // /// <summary>
    // /// Get applications by research ID
    // /// </summary>
    // Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);
}
