using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Data.Interfaces.Clinical;

/// <summary>
/// Repository interface for clinical event operations
/// </summary>
public interface IClinicalEventRepository : IBaseRepository<ClinicalEvent, string>
{
    /// <summary>
    /// Get active clinical events
    /// </summary>
    Task<List<ClinicalEvent>> GetActiveAsync(CancellationToken cancellationToken = default);
}
