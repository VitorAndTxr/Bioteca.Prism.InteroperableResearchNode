using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Domain.DTOs.Volunteer;
using Bioteca.Prism.Domain.Payloads.Volunteer;
using Bioteca.Prism.Service.Interfaces.Volunteer;

namespace Bioteca.Prism.Service.Services.Volunteer;

/// <summary>
/// Service implementation for volunteer operations
/// </summary>
public class VolunteerService : BaseService<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;

    public VolunteerService(IVolunteerRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _volunteerRepository = repository;
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
            MedicalHistory = payload.MedicalHistory,
            ConsentStatus = payload.ConsentStatus,
            EnrolledAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _volunteerRepository.AddAsync(volunteer);
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

        if (!string.IsNullOrEmpty(payload.MedicalHistory))
        {
            volunteer.MedicalHistory = payload.MedicalHistory;
        }

        if (!string.IsNullOrEmpty(payload.ConsentStatus))
        {
            volunteer.ConsentStatus = payload.ConsentStatus;
        }

        volunteer.UpdatedAt = DateTime.UtcNow;

        await _volunteerRepository.UpdateAsync(volunteer);

        return MapToDTO(volunteer);
    }

    public async Task<bool> DeleteVolunteerAsync(Guid id)
    {
        return await _volunteerRepository.DeleteAsync(id);
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
            MedicalHistory = volunteer.MedicalHistory,
            ConsentStatus = volunteer.ConsentStatus,
            EnrolledAt = volunteer.EnrolledAt,
            UpdatedAt = volunteer.UpdatedAt
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
