using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Researcher;

/// <summary>
/// Repository interface for researcher persistence operations
/// </summary>
public interface IResearcherRepository : IBaseRepository<Domain.Entities.Researcher.Researcher, Guid>
{
    /// <summary>
    /// Get researcher by email
    /// </summary>
    Task<Domain.Entities.Researcher.Researcher?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get researchers by node ID
    /// </summary>
    Task<List<Domain.Entities.Researcher.Researcher>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get researchers by institution
    /// </summary>
    Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution, CancellationToken cancellationToken = default);
}
