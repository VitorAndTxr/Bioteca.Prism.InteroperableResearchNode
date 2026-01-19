using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
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

    /// <summary>
    /// Get active allergies/intolerances as DTOs
    /// </summary>
    Task<List<SnomedAllergyIntoleranceDTO>> GetActiveAllergyIntolerancesAsync();

    /// <summary>
    /// Get all allergies/intolerances with pagination
    /// </summary>
    Task<List<SnomedAllergyIntoleranceDTO>> GetAllAllergyIntolerancesPaginateAsync();

    /// <summary>
    /// Add a new allergy/intolerance
    /// </summary>
    Task<AllergyIntolerance> AddAsync(SnomedAllergyIntoleranceDTO payload);

    /// <summary>
    /// Get an allergy/intolerance by SNOMED code
    /// </summary>
    Task<SnomedAllergyIntoleranceDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an allergy/intolerance by SNOMED code
    /// </summary>
    Task<SnomedAllergyIntoleranceDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedAllergyIntoleranceDTO payload, CancellationToken cancellationToken = default);
}
