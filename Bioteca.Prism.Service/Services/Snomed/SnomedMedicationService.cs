using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service implementation for SNOMED CT medication operations
/// </summary>
public class SnomedMedicationService : BaseService<Medication, string>, ISnomedMedicationService
{
    private readonly IMedicationRepository _medicationRepository;

    public SnomedMedicationService(IMedicationRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _medicationRepository = repository;
    }

    public async Task<List<SnomedMedicationDTO>> GetActiveAsync()
    {
        var allMedications = await _medicationRepository.GetAllAsync();
        return allMedications.Where(m => m.IsActive)
            .Select(medication => new SnomedMedicationDTO
            {
                SnomedCode = medication.SnomedCode,
                MedicationName = medication.MedicationName,
                ActiveIngredient = medication.ActiveIngredient,
                AnvisaCode = medication.AnvisaCode
            }).ToList();
    }

    public async Task<List<SnomedMedicationDTO>> GetAllMedicationsPaginateAsync()
    {
        var result = await _medicationRepository.GetPagedAsync();

        return result.Where(x => x.IsActive)
            .Select(medication => new SnomedMedicationDTO
            {
                SnomedCode = medication.SnomedCode,
                MedicationName = medication.MedicationName,
                ActiveIngredient = medication.ActiveIngredient,
                AnvisaCode = medication.AnvisaCode
            }).ToList();
    }

    public async Task<Medication> AddAsync(SnomedMedicationDTO payload)
    {
        ValidateAddMedicationPayload(payload);

        Medication newMedication = new Medication
        {
            SnomedCode = payload.SnomedCode,
            MedicationName = payload.MedicationName,
            ActiveIngredient = payload.ActiveIngredient,
            AnvisaCode = payload.AnvisaCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _medicationRepository.AddAsync(newMedication);
    }

    private void ValidateAddMedicationPayload(SnomedMedicationDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("SnomedCode is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.MedicationName))
        {
            throw new ArgumentException("MedicationName is required.");
        }

        if (_medicationRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("A medication with the same SnomedCode already exists.");
        }
    }

    public async Task<SnomedMedicationDTO?> GetBySnomedCodeAsync(string snomedCode)
    {
        var medication = await _medicationRepository.GetByIdAsync(snomedCode);

        if (medication == null)
        {
            return null;
        }

        return new SnomedMedicationDTO
        {
            SnomedCode = medication.SnomedCode,
            MedicationName = medication.MedicationName,
            ActiveIngredient = medication.ActiveIngredient,
            AnvisaCode = medication.AnvisaCode
        };
    }

    public async Task<SnomedMedicationDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedMedicationDTO payload)
    {
        var existingMedication = await _medicationRepository.GetByIdAsync(snomedCode);

        if (existingMedication == null)
        {
            return null;
        }

        ValidateUpdateMedicationPayload(payload);

        existingMedication.MedicationName = payload.MedicationName;
        existingMedication.ActiveIngredient = payload.ActiveIngredient;
        existingMedication.AnvisaCode = payload.AnvisaCode;
        existingMedication.UpdatedAt = DateTime.UtcNow;

        await _medicationRepository.UpdateAsync(existingMedication);

        return new SnomedMedicationDTO
        {
            SnomedCode = existingMedication.SnomedCode,
            MedicationName = existingMedication.MedicationName,
            ActiveIngredient = existingMedication.ActiveIngredient,
            AnvisaCode = existingMedication.AnvisaCode
        };
    }

    private void ValidateUpdateMedicationPayload(UpdateSnomedMedicationDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.MedicationName))
        {
            throw new ArgumentException("MedicationName is required.");
        }
    }
}
