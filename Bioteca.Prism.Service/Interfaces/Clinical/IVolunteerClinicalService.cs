using Bioteca.Prism.Domain.Entities.Volunteer;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Aggregate service interface for volunteer clinical data operations
/// </summary>
public interface IVolunteerClinicalService
{
    // Volunteer Clinical Conditions
    Task<List<VolunteerClinicalCondition>> GetVolunteerConditionsAsync(Guid volunteerId, CancellationToken cancellationToken = default);
    Task<VolunteerClinicalCondition> AddConditionAsync(VolunteerClinicalCondition condition, CancellationToken cancellationToken = default);
    Task<VolunteerClinicalCondition> UpdateConditionAsync(VolunteerClinicalCondition condition, CancellationToken cancellationToken = default);

    // Volunteer Clinical Events
    Task<List<VolunteerClinicalEvent>> GetVolunteerEventsAsync(Guid volunteerId, CancellationToken cancellationToken = default);
    Task<VolunteerClinicalEvent> AddEventAsync(VolunteerClinicalEvent clinicalEvent, CancellationToken cancellationToken = default);

    // Volunteer Medications
    Task<List<VolunteerMedication>> GetVolunteerMedicationsAsync(Guid volunteerId, CancellationToken cancellationToken = default);
    Task<List<VolunteerMedication>> GetActiveMedicationsAsync(Guid volunteerId, CancellationToken cancellationToken = default);
    Task<VolunteerMedication> AddMedicationAsync(VolunteerMedication medication, CancellationToken cancellationToken = default);

    // Volunteer Allergies/Intolerances
    Task<List<VolunteerAllergyIntolerance>> GetVolunteerAllergiesAsync(Guid volunteerId, CancellationToken cancellationToken = default);
    Task<VolunteerAllergyIntolerance> AddAllergyAsync(VolunteerAllergyIntolerance allergy, CancellationToken cancellationToken = default);

    // Clinical Summary
    Task<object> GetClinicalSummaryAsync(Guid volunteerId, CancellationToken cancellationToken = default);
}
