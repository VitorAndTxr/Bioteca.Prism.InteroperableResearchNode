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
    public async Task<List<VolunteerClinicalCondition>> GetVolunteerConditionsAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allConditions = await _conditionRepository.GetAllAsync(cancellationToken);
        return allConditions.Where(c => c.VolunteerId == volunteerId).ToList();
    }

    public async Task<VolunteerClinicalCondition> AddConditionAsync(VolunteerClinicalCondition condition, CancellationToken cancellationToken = default)
    {
        return await _conditionRepository.AddAsync(condition, cancellationToken);
    }

    public async Task<VolunteerClinicalCondition> UpdateConditionAsync(VolunteerClinicalCondition condition, CancellationToken cancellationToken = default)
    {
        return await _conditionRepository.UpdateAsync(condition, cancellationToken);
    }

    // Volunteer Clinical Events
    public async Task<List<VolunteerClinicalEvent>> GetVolunteerEventsAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
        return allEvents
            .Where(e => e.VolunteerId == volunteerId)
            .OrderByDescending(e => e.EventDatetime)
            .ToList();
    }

    public async Task<VolunteerClinicalEvent> AddEventAsync(VolunteerClinicalEvent clinicalEvent, CancellationToken cancellationToken = default)
    {
        return await _eventRepository.AddAsync(clinicalEvent, cancellationToken);
    }

    // Volunteer Medications
    public async Task<List<VolunteerMedication>> GetVolunteerMedicationsAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allMedications = await _medicationRepository.GetAllAsync(cancellationToken);
        return allMedications.Where(m => m.VolunteerId == volunteerId).ToList();
    }

    public async Task<List<VolunteerMedication>> GetActiveMedicationsAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allMedications = await _medicationRepository.GetAllAsync(cancellationToken);
        return allMedications
            .Where(m => m.VolunteerId == volunteerId && m.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<VolunteerMedication> AddMedicationAsync(VolunteerMedication medication, CancellationToken cancellationToken = default)
    {
        return await _medicationRepository.AddAsync(medication, cancellationToken);
    }

    // Volunteer Allergies/Intolerances
    public async Task<List<VolunteerAllergyIntolerance>> GetVolunteerAllergiesAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allAllergies = await _allergyRepository.GetAllAsync(cancellationToken);
        return allAllergies.Where(a => a.VolunteerId == volunteerId).ToList();
    }

    public async Task<VolunteerAllergyIntolerance> AddAllergyAsync(VolunteerAllergyIntolerance allergy, CancellationToken cancellationToken = default)
    {
        return await _allergyRepository.AddAsync(allergy, cancellationToken);
    }

    // Clinical Summary
    public async Task<object> GetClinicalSummaryAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var conditions = await GetVolunteerConditionsAsync(volunteerId, cancellationToken);
        var events = await GetVolunteerEventsAsync(volunteerId, cancellationToken);
        var medications = await GetVolunteerMedicationsAsync(volunteerId, cancellationToken);
        var allergies = await GetVolunteerAllergiesAsync(volunteerId, cancellationToken);

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
