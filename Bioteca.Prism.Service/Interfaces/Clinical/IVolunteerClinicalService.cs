using Bioteca.Prism.Domain.Entities.Volunteer;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Aggregate service interface for volunteer clinical data operations
/// </summary>
public interface IVolunteerClinicalService
{
    // Volunteer Clinical Conditions
    Task<List<VolunteerClinicalCondition>> GetVolunteerConditionsAsync(Guid volunteerId);
    Task<VolunteerClinicalCondition> AddConditionAsync(VolunteerClinicalCondition condition);
    Task<VolunteerClinicalCondition> UpdateConditionAsync(VolunteerClinicalCondition condition);

    // Volunteer Clinical Events
    Task<List<VolunteerClinicalEvent>> GetVolunteerEventsAsync(Guid volunteerId);
    Task<VolunteerClinicalEvent> AddEventAsync(VolunteerClinicalEvent clinicalEvent);

    // Volunteer Medications
    Task<List<VolunteerMedication>> GetVolunteerMedicationsAsync(Guid volunteerId);
    Task<List<VolunteerMedication>> GetActiveMedicationsAsync(Guid volunteerId);
    Task<VolunteerMedication> AddMedicationAsync(VolunteerMedication medication);

    // Volunteer Allergies/Intolerances
    Task<List<VolunteerAllergyIntolerance>> GetVolunteerAllergiesAsync(Guid volunteerId);
    Task<VolunteerAllergyIntolerance> AddAllergyAsync(VolunteerAllergyIntolerance allergy);

    // Clinical Summary
    Task<object> GetClinicalSummaryAsync(Guid volunteerId);
}
