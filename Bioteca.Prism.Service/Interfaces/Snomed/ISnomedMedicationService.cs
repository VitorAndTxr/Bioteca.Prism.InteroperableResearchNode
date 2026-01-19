using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Clinical;

namespace Bioteca.Prism.Service.Interfaces.Snomed;

/// <summary>
/// Service interface for SNOMED CT medication operations
/// </summary>
public interface ISnomedMedicationService : IServiceBase<Medication, string>
{
    /// <summary>
    /// Get active medications
    /// </summary>
    Task<List<SnomedMedicationDTO>> GetActiveAsync();

    /// <summary>
    /// Get all medications with pagination
    /// </summary>
    Task<List<SnomedMedicationDTO>> GetAllMedicationsPaginateAsync();

    /// <summary>
    /// Add a new medication
    /// </summary>
    Task<Medication> AddAsync(SnomedMedicationDTO payload);

    /// <summary>
    /// Get a medication by SNOMED code
    /// </summary>
    Task<SnomedMedicationDTO?> GetBySnomedCodeAsync(string snomedCode);

    /// <summary>
    /// Update a medication by SNOMED code
    /// </summary>
    Task<SnomedMedicationDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedMedicationDTO payload);
}
