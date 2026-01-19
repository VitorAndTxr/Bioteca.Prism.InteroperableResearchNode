using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Data.Interfaces.Clinical;

/// <summary>
/// Repository interface for allergy/intolerance operations
/// </summary>
public interface IAllergyIntoleranceRepository : IBaseRepository<AllergyIntolerance, string>
{
    /// <summary>
    /// Get active allergies/intolerances
    /// </summary>
    Task<List<AllergyIntolerance>> GetActiveAsync(CancellationToken cancellationToken = default);
}
