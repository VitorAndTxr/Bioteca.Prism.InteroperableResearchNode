using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Domain.DTOs.Volunteer;
using Bioteca.Prism.Domain.Entities.Volunteer;
using Bioteca.Prism.Domain.Payloads.Volunteer;
using Bioteca.Prism.Service.Interfaces.Volunteer;

namespace Bioteca.Prism.Service.Services.Volunteer;

/// <summary>
/// Service implementation for volunteer operations
/// </summary>
public class VolunteerService : BaseService<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IBaseRepository<VolunteerClinicalCondition, Guid> _conditionRepository;
    private readonly IBaseRepository<VolunteerClinicalEvent, Guid> _eventRepository;
    private readonly IBaseRepository<VolunteerMedication, Guid> _medicationRepository;
    private readonly IBaseRepository<VolunteerAllergyIntolerance, Guid> _allergyRepository;

    public VolunteerService(
        IVolunteerRepository repository,
        IApiContext apiContext,
        IBaseRepository<VolunteerClinicalCondition, Guid> conditionRepository,
        IBaseRepository<VolunteerClinicalEvent, Guid> eventRepository,
        IBaseRepository<VolunteerMedication, Guid> medicationRepository,
        IBaseRepository<VolunteerAllergyIntolerance, Guid> allergyRepository)
        : base(repository, apiContext)
    {
        _volunteerRepository = repository;
        _conditionRepository = conditionRepository;
        _eventRepository = eventRepository;
        _medicationRepository = medicationRepository;
        _allergyRepository = allergyRepository;
    }

    public async Task<List<Domain.Entities.Volunteer.Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _volunteerRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default)
    {
        return await _volunteerRepository.GetByVolunteerCodeAsync(volunteerCode, cancellationToken);
    }

    public async Task<List<VolunteerDTO>> GetAllPaginateAsync()
    {
        var result = await _volunteerRepository.GetPagedAsync();

        var mappedResult = result.Select(MapToDTO).ToList();

        return mappedResult;
    }

    public async Task<VolunteerDTO?> GetVolunteerByIdAsync(Guid id)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(id);

        if (volunteer == null)
        {
            return null;
        }

        return MapToDTO(volunteer);
    }

    public async Task<Domain.Entities.Volunteer.Volunteer> AddAsync(AddVolunteerPayload payload)
    {
        ValidateAddPayload(payload);

        var volunteer = new Domain.Entities.Volunteer.Volunteer
        {
            VolunteerId = Guid.NewGuid(),
            Name = payload.Name,
            Email = payload.Email,
            VolunteerCode = payload.VolunteerCode,
            ResearchNodeId = payload.ResearchNodeId,
            BirthDate = DateTime.SpecifyKind(payload.BirthDate, DateTimeKind.Utc),
            Gender = payload.Gender,
            BloodType = payload.BloodType,
            Height = payload.Height,
            Weight = payload.Weight,
            ConsentStatus = payload.ConsentStatus,
            EnrolledAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _volunteerRepository.AddAsync(volunteer);

        var now = DateTime.UtcNow;
        var recordedBy = _apiContext.SecurityContext.User?.ResearcherId ?? Guid.Empty;

        if (payload.ClinicalConditionCodes != null)
        {
            foreach (var code in payload.ClinicalConditionCodes)
            {
                await _conditionRepository.AddAsync(new VolunteerClinicalCondition
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteer.VolunteerId,
                    SnomedCode = code,
                    ClinicalStatus = "active",
                    RecordedBy = recordedBy,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (payload.ClinicalEventCodes != null)
        {
            foreach (var code in payload.ClinicalEventCodes)
            {
                await _eventRepository.AddAsync(new VolunteerClinicalEvent
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteer.VolunteerId,
                    SnomedCode = code,
                    EventType = "finding",
                    EventDatetime = now,
                    ValueUnit = string.Empty,
                    Characteristics = string.Empty,
                    RecordedBy = recordedBy,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (payload.MedicationCodes != null)
        {
            foreach (var code in payload.MedicationCodes)
            {
                await _medicationRepository.AddAsync(new VolunteerMedication
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteer.VolunteerId,
                    MedicationSnomedCode = code,
                    Dosage = string.Empty,
                    Frequency = string.Empty,
                    Route = string.Empty,
                    StartDate = now,
                    Status = "active",
                    Notes = string.Empty,
                    RecordedBy = recordedBy,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (payload.AllergyIntoleranceCodes != null)
        {
            foreach (var code in payload.AllergyIntoleranceCodes)
            {
                await _allergyRepository.AddAsync(new VolunteerAllergyIntolerance
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteer.VolunteerId,
                    AllergyIntoleranceSnomedCode = code,
                    Criticality = "unable-to-assess",
                    ClinicalStatus = "active",
                    Manifestations = string.Empty,
                    VerificationStatus = "unconfirmed",
                    RecordedBy = recordedBy,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        return volunteer;
    }

    public async Task<VolunteerDTO?> UpdateVolunteerAsync(Guid id, UpdateVolunteerPayload payload)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(id);

        if (volunteer == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(payload.Name))
        {
            volunteer.Name = payload.Name;
        }

        if (!string.IsNullOrEmpty(payload.Email))
        {
            volunteer.Email = payload.Email;
        }

        if (payload.BirthDate.HasValue)
        {
            volunteer.BirthDate = DateTime.SpecifyKind(payload.BirthDate.Value, DateTimeKind.Utc);
        }

        if (!string.IsNullOrEmpty(payload.Gender))
        {
            volunteer.Gender = payload.Gender;
        }

        if (!string.IsNullOrEmpty(payload.BloodType))
        {
            volunteer.BloodType = payload.BloodType;
        }

        if (payload.Height.HasValue)
        {
            volunteer.Height = payload.Height.Value;
        }

        if (payload.Weight.HasValue)
        {
            volunteer.Weight = payload.Weight.Value;
        }

        if (!string.IsNullOrEmpty(payload.ConsentStatus))
        {
            volunteer.ConsentStatus = payload.ConsentStatus;
        }

        volunteer.UpdatedAt = DateTime.UtcNow;

        await _volunteerRepository.UpdateAsync(volunteer);

        await MergeClinicalConditionsAsync(id, payload.ClinicalConditionCodes);
        await MergeClinicalEventsAsync(id, payload.ClinicalEventCodes);
        await MergeMedicationsAsync(id, payload.MedicationCodes);
        await MergeAllergyIntolerancesAsync(id, payload.AllergyIntoleranceCodes);

        // Reload volunteer with clinical collections to return accurate DTO
        var updated = await _volunteerRepository.GetByIdAsync(id);
        return MapToDTO(updated!);
    }

    public async Task<bool> DeleteVolunteerAsync(Guid id)
    {
        return await _volunteerRepository.DeleteAsync(id);
    }

    private async Task MergeClinicalConditionsAsync(Guid volunteerId, List<string>? desiredCodes)
    {
        try
        {

            // null means caller did not touch this collection
            if (desiredCodes == null) return;

            var existing = await _conditionRepository.FindAsync(c => c.VolunteerId == volunteerId);

            var existingCodes = existing.Select(c => c.SnomedCode).ToHashSet();
            var desired = desiredCodes.ToHashSet();
            var now = DateTime.UtcNow;
            var recordedBy = _apiContext.SecurityContext.User?.ResearcherId ?? Guid.Empty;

            foreach (var code in desired.Except(existingCodes))
            {
                await _conditionRepository.AddAsync(new VolunteerClinicalCondition
                {
                    Id = Guid.NewGuid(),
                    VolunteerId = volunteerId,
                    SnomedCode = code,
                    ClinicalStatus = "active",
                    RecordedBy = recordedBy,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            foreach (var toRemove in existing.Where(c => !desired.Contains(c.SnomedCode)))
            {
                await _conditionRepository.DeleteAsync(toRemove.Id);
            }
        }catch(Exception ex)
        {
            // Log and rethrow to ensure API returns 500 instead of silently failing
            // In a real implementation, consider more granular error handling and logging
            Console.Error.WriteLine($"Error merging clinical conditions for volunteer {volunteerId}: {ex}");
            throw;
        }
    }

    private async Task MergeClinicalEventsAsync(Guid volunteerId, List<string>? desiredCodes)
    {
        if (desiredCodes == null) return;

        var existing = await _eventRepository.FindAsync(e => e.VolunteerId == volunteerId);

        var existingCodes = existing.Select(e => e.SnomedCode).ToHashSet();
        var desired = desiredCodes.ToHashSet();
        var now = DateTime.UtcNow;
        var recordedBy = _apiContext.SecurityContext.User?.ResearcherId ?? Guid.Empty;

        foreach (var code in desired.Except(existingCodes))
        {
            await _eventRepository.AddAsync(new VolunteerClinicalEvent
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                SnomedCode = code,
                EventType = "finding",
                EventDatetime = now,
                ValueUnit = string.Empty,
                Characteristics = string.Empty,
                RecordedBy = recordedBy,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var toRemove in existing.Where(e => !desired.Contains(e.SnomedCode)))
        {
            await _eventRepository.DeleteAsync(toRemove.Id);
        }
    }

    private async Task MergeMedicationsAsync(Guid volunteerId, List<string>? desiredCodes)
    {
        if (desiredCodes == null) return;

        var existing = await _medicationRepository.FindAsync(m => m.VolunteerId == volunteerId);

        var existingCodes = existing.Select(m => m.MedicationSnomedCode).ToHashSet();
        var desired = desiredCodes.ToHashSet();
        var now = DateTime.UtcNow;
        var recordedBy = _apiContext.SecurityContext.User?.ResearcherId ?? Guid.Empty;

        foreach (var code in desired.Except(existingCodes))
        {
            await _medicationRepository.AddAsync(new VolunteerMedication
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                MedicationSnomedCode = code,
                Dosage = string.Empty,
                Frequency = string.Empty,
                Route = string.Empty,
                StartDate = now,
                Status = "active",
                Notes = string.Empty,
                RecordedBy = recordedBy,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var toRemove in existing.Where(m => !desired.Contains(m.MedicationSnomedCode)))
        {
            await _medicationRepository.DeleteAsync(toRemove.Id);
        }
    }

    private async Task MergeAllergyIntolerancesAsync(Guid volunteerId, List<string>? desiredCodes)
    {
        if (desiredCodes == null) return;

        var existing = await _allergyRepository.FindAsync(a => a.VolunteerId == volunteerId);

        var existingCodes = existing.Select(a => a.AllergyIntoleranceSnomedCode).ToHashSet();
        var desired = desiredCodes.ToHashSet();
        var now = DateTime.UtcNow;
        var recordedBy = _apiContext.SecurityContext.User?.ResearcherId ?? Guid.Empty;

        foreach (var code in desired.Except(existingCodes))
        {
            await _allergyRepository.AddAsync(new VolunteerAllergyIntolerance
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteerId,
                AllergyIntoleranceSnomedCode = code,
                Criticality = "unable-to-assess",
                ClinicalStatus = "active",
                Manifestations = string.Empty,
                VerificationStatus = "unconfirmed",
                RecordedBy = recordedBy,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var toRemove in existing.Where(a => !desired.Contains(a.AllergyIntoleranceSnomedCode)))
        {
            await _allergyRepository.DeleteAsync(toRemove.Id);
        }
    }

    private static VolunteerDTO MapToDTO(Domain.Entities.Volunteer.Volunteer volunteer)
    {
        return new VolunteerDTO
        {
            VolunteerId = volunteer.VolunteerId,
            ResearchNodeId = volunteer.ResearchNodeId,
            VolunteerCode = volunteer.VolunteerCode,
            Name = volunteer.Name,
            Email = volunteer.Email,
            BirthDate = volunteer.BirthDate,
            Gender = volunteer.Gender,
            BloodType = volunteer.BloodType,
            Height = volunteer.Height,
            Weight = volunteer.Weight,
            ConsentStatus = volunteer.ConsentStatus,
            EnrolledAt = volunteer.EnrolledAt,
            UpdatedAt = volunteer.UpdatedAt,
            ClinicalConditionCodes = volunteer.ClinicalConditions?.Select(c => c.SnomedCode).ToList() ?? new(),
            ClinicalEventCodes = volunteer.ClinicalEvents?.Select(e => e.SnomedCode).ToList() ?? new(),
            MedicationCodes = volunteer.Medications?.Select(m => m.MedicationSnomedCode).ToList() ?? new(),
            AllergyIntoleranceCodes = volunteer.AllergyIntolerances?.Select(a => a.AllergyIntoleranceSnomedCode).ToList() ?? new()
        };
    }

    private void ValidateAddPayload(AddVolunteerPayload payload)
    {
        if (string.IsNullOrEmpty(payload.Name))
        {
            throw new Exception("Volunteer name is required");
        }

        if (string.IsNullOrEmpty(payload.Email))
        {
            throw new Exception("Volunteer email is required");
        }

        if (payload.ResearchNodeId == Guid.Empty)
        {
            throw new Exception("Research node ID is required");
        }

        if (string.IsNullOrEmpty(payload.Gender))
        {
            throw new Exception("Gender is required");
        }

        // Auto-generate VolunteerCode if not provided
        if (string.IsNullOrEmpty(payload.VolunteerCode))
        {
            payload.VolunteerCode = $"VC-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        }

        // Default BloodType to Unknown if not provided
        if (string.IsNullOrEmpty(payload.BloodType))
        {
            payload.BloodType = "Unknown";
        }

        // Default ConsentStatus to Pending if not provided
        if (string.IsNullOrEmpty(payload.ConsentStatus))
        {
            payload.ConsentStatus = "Pending";
        }
    }
}
