using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Data.Interfaces.Clinical;

/// <summary>
/// Repository interface for medication operations
/// </summary>
public interface IMedicationRepository : IBaseRepository<Medication, string>
{
    /// <summary>
    /// Get active medications
    /// </summary>
    Task<List<Medication>> GetActiveAsync(CancellationToken cancellationToken = default);
}
