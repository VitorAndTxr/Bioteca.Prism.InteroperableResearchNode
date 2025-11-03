using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Service interface for clinical condition operations
/// </summary>
public interface IClinicalConditionService : IServiceBase<ClinicalCondition, string>
{
    /// <summary>
    /// Get active clinical conditions
    /// </summary>
    Task<List<ClinicalCondition>> GetActiveConditionsAsync();

    /// <summary>
    /// Search conditions by display name
    /// </summary>
    Task<List<ClinicalCondition>> SearchByNameAsync(string searchTerm);
}
