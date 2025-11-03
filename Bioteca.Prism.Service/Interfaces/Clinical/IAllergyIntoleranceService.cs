using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Service interface for allergy/intolerance operations
/// </summary>
public interface IAllergyIntoleranceService : IServiceBase<AllergyIntolerance, string>
{
    /// <summary>
    /// Get active allergies/intolerances
    /// </summary>
    Task<List<AllergyIntolerance>> GetActiveAsync();

    /// <summary>
    /// Get allergies/intolerances by category
    /// </summary>
    Task<List<AllergyIntolerance>> GetByCategoryAsync(string category);

    /// <summary>
    /// Get allergies/intolerances by type
    /// </summary>
    Task<List<AllergyIntolerance>> GetByTypeAsync(string type);
}
