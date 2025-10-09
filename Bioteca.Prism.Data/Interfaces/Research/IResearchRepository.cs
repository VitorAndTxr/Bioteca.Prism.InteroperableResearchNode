using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Research;

/// <summary>
/// Repository interface for research persistence operations
/// </summary>
public interface IResearchRepository : IBaseRepository<Domain.Entities.Research.Research, Guid>
{
    /// <summary>
    /// Get research projects by node ID
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get research projects by status
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active research projects
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default);
}
