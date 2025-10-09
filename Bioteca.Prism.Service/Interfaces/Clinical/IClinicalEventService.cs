using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Service interface for clinical event operations
/// </summary>
public interface IClinicalEventService : IServiceBase<ClinicalEvent, string>
{
    /// <summary>
    /// Get active clinical events
    /// </summary>
    Task<List<ClinicalEvent>> GetActiveEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search events by display name
    /// </summary>
    Task<List<ClinicalEvent>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}
