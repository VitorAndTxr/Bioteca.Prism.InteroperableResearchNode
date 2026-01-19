using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
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
    Task<List<SnomedClinicalEventDTO>> GetActiveEventsAsync();

    /// <summary>
    /// Search events by display name
    /// </summary>
    Task<List<ClinicalEvent>> SearchByNameAsync(string searchTerm);

    /// <summary>
    /// Get all clinical events with pagination
    /// </summary>
    Task<List<SnomedClinicalEventDTO>> GetAllClinicalEventsPaginateAsync();

    /// <summary>
    /// Add a new clinical event
    /// </summary>
    Task<ClinicalEvent> AddAsync(SnomedClinicalEventDTO payload);

    /// <summary>
    /// Get a clinical event by SNOMED code
    /// </summary>
    Task<SnomedClinicalEventDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a clinical event by SNOMED code
    /// </summary>
    Task<SnomedClinicalEventDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedClinicalEventDTO payload, CancellationToken cancellationToken = default);
}
