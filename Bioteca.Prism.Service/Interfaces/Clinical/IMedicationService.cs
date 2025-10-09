using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Service interface for medication operations
/// </summary>
public interface IMedicationService : IServiceBase<Medication, string>
{
    /// <summary>
    /// Get active medications
    /// </summary>
    Task<List<Medication>> GetActiveMedicationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search medications by name or active ingredient
    /// </summary>
    Task<List<Medication>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get medication by ANVISA code
    /// </summary>
    Task<Medication?> GetByAnvisaCodeAsync(string anvisaCode, CancellationToken cancellationToken = default);
}
