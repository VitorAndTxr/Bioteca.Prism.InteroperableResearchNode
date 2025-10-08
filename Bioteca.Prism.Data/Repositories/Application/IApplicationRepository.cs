using Bioteca.Prism.Domain.Entities.Application;

namespace Bioteca.Prism.Data.Repositories.Application;

/// <summary>
/// Repository interface for application persistence operations
/// </summary>
public interface IApplicationRepository : IRepository<Domain.Entities.Application.Application, Guid>
{
    /// <summary>
    /// Get applications by research ID
    /// </summary>
    Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);
}
