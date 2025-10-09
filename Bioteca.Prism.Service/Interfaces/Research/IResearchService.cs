using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Service.Interfaces.Research;

/// <summary>
/// Service interface for research project operations
/// </summary>
public interface IResearchService : IServiceBase<Domain.Entities.Research.Research, Guid>
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
    /// Get active research projects (no end date or end date in future)
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default);
}
