using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Volunteer;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Aggregate service implementation for volunteer clinical data operations
/// </summary>
public class VolunteerClinicalService : IVolunteerClinicalService
{
    private readonly IBaseRepository<VolunteerClinicalCondition, Guid> _conditionRepository;
    private readonly IBaseRepository<VolunteerClinicalEvent, Guid> _eventRepository;
    private readonly IBaseRepository<VolunteerMedication, Guid> _medicationRepository;
    private readonly IBaseRepository<VolunteerAllergyIntolerance, Guid> _allergyRepository;

    public VolunteerClinicalService(
        IBaseRepository<VolunteerClinicalCondition, Guid> conditionRepository,
        IBaseRepository<VolunteerClinicalEvent, Guid> eventRepository,
        IBaseRepository<VolunteerMedication, Guid> medicationRepository,
        IBaseRepository<VolunteerAllergyIntolerance, Guid> allergyRepository)
    {
        _conditionRepository = conditionRepository;
        _eventRepository = eventRepository;
        _medicationRepository = medicationRepository;
        _allergyRepository = allergyRepository;
    }

    // Volunteer Clinical Conditions
    public async Task<List<VolunteerClinicalCondition>> GetVolunteerConditionsAsync(Guid volunteerId)
    {
        var allConditions = await _conditionRepository.GetAllAsync();
        return allConditions.Where(c => c.VolunteerId == volunteerId).ToList();
    }

    public async Task<VolunteerClinicalCondition> AddConditionAsync(VolunteerClinicalCondition condition)
    {
        return await _conditionRepository.AddAsync(condition);
    }

    public async Task<VolunteerClinicalCondition> UpdateConditionAsync(VolunteerClinicalCondition condition)
    {
        return await _conditionRepository.UpdateAsync(condition);
    }

    // Volunteer Clinical Events
    public async Task<List<VolunteerClinicalEvent>> GetVolunteerEventsAsync(Guid volunteerId)
    {
        var allEvents = await _eventRepository.GetAllAsync();
        return allEvents
            .Where(e => e.VolunteerId == volunteerId)
            .OrderByDescending(e => e.EventDatetime)
            .ToList();
    }

    public async Task<VolunteerClinicalEvent> AddEventAsync(VolunteerClinicalEvent clinicalEvent)
    {
        return await _eventRepository.AddAsync(clinicalEvent);
    }

    // Volunteer Medications
    public async Task<List<VolunteerMedication>> GetVolunteerMedicationsAsync(Guid volunteerId)
    {
        var allMedications = await _medicationRepository.GetAllAsync();
        return allMedications.Where(m => m.VolunteerId == volunteerId).ToList();
    }

    public async Task<List<VolunteerMedication>> GetActiveMedicationsAsync(Guid volunteerId)
    {
        var allMedications = await _medicationRepository.GetAllAsync();
        return allMedications
            .Where(m => m.VolunteerId == volunteerId && m.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<VolunteerMedication> AddMedicationAsync(VolunteerMedication medication)
    {
        return await _medicationRepository.AddAsync(medication);
    }

    // Volunteer Allergies/Intolerances
    public async Task<List<VolunteerAllergyIntolerance>> GetVolunteerAllergiesAsync(Guid volunteerId)
    {
        var allAllergies = await _allergyRepository.GetAllAsync();
        return allAllergies.Where(a => a.VolunteerId == volunteerId).ToList();
    }

    public async Task<VolunteerAllergyIntolerance> AddAllergyAsync(VolunteerAllergyIntolerance allergy)
    {
        return await _allergyRepository.AddAsync(allergy);
    }

    // Clinical Summary
    public async Task<object> GetClinicalSummaryAsync(Guid volunteerId)
    {
        var conditions = await GetVolunteerConditionsAsync(volunteerId);
        var events = await GetVolunteerEventsAsync(volunteerId);
        var medications = await GetVolunteerMedicationsAsync(volunteerId);
        var allergies = await GetVolunteerAllergiesAsync(volunteerId);

        return new
        {
            VolunteerId = volunteerId,
            ActiveConditions = conditions.Where(c => c.ClinicalStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList(),
            RecentEvents = events.Take(10).ToList(),
            ActiveMedications = medications.Where(m => m.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList(),
            KnownAllergies = allergies.Where(a => a.ClinicalStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList()
        };
    }
}
