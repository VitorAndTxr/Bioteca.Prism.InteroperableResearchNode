using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Data.Interfaces.Clinical;

/// <summary>
/// Repository interface for clinical condition operations
/// </summary>
public interface IClinicalConditionRepository : IBaseRepository<ClinicalCondition, string>
{
    /// <summary>
    /// Get active clinical conditions
    /// </summary>
    Task<List<ClinicalCondition>> GetActiveAsync(CancellationToken cancellationToken = default);
}
