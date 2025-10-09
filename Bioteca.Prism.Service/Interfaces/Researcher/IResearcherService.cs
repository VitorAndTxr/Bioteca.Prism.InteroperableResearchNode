using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Service.Interfaces.Researcher;

/// <summary>
/// Service interface for researcher operations
/// </summary>
public interface IResearcherService : IServiceBase<Domain.Entities.Researcher.Researcher, Guid>
{
    /// <summary>
    /// Get researchers by node ID
    /// </summary>
    Task<List<Domain.Entities.Researcher.Researcher>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get researchers by institution
    /// </summary>
    Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution, CancellationToken cancellationToken = default);
}
