using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Domain.DTOs.Application;
using Bioteca.Prism.Domain.DTOs.Research;
using Bioteca.Prism.Domain.DTOs.ResearchNode;
using Bioteca.Prism.Domain.DTOs.Sensor;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Payloads.Application;
using Bioteca.Prism.Domain.Payloads.Research;
using Bioteca.Prism.Service.Interfaces.Research;

namespace Bioteca.Prism.Service.Services.Research;

/// <summary>
/// Service implementation for research project operations
/// </summary>
public class ResearchService : BaseService<Domain.Entities.Research.Research, Guid>, IResearchService
{
    private readonly IResearchRepository _researchRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly IResearcherRepository _researcherRepository;
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IDeviceRepository _deviceRepository;

    private static readonly string[] ValidStatuses = { "Planning", "Active", "Completed", "Suspended", "Cancelled" };
    private static readonly string[] ValidEnrollmentStatuses = { "Enrolled", "Active", "Completed", "Withdrawn", "Excluded" };
    private static readonly string[] ValidCalibrationStatuses = { "NotCalibrated", "Calibrated", "Expired", "InProgress" };

    public ResearchService(
        IResearchRepository repository,
        INodeRepository nodeRepository,
        IResearcherRepository researcherRepository,
        IVolunteerRepository volunteerRepository,
        IDeviceRepository deviceRepository,
        IApiContext apiContext) : base(repository, apiContext)
    {
        _researchRepository = repository;
        _nodeRepository = nodeRepository;
        _researcherRepository = researcherRepository;
        _volunteerRepository = volunteerRepository;
        _deviceRepository = deviceRepository;
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByStatusAsync(status, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetActiveResearchAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Research.Research> AddAsync(AddResearchDTO payload)
    {
        ValidateAddResearchPayload(payload);

        var research = new Domain.Entities.Research.Research
        {
            Id = Guid.NewGuid(),
            Title = payload.Title,
            Description = payload.Description,
            ResearchNodeId = payload.ResearchNodeId,
            StartDate = DateTime.UtcNow,
            Status = "Planning",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _researchRepository.AddAsync(research);
    }

    public async Task<List<ResearchDTO>> GetAllPaginateAsync()
    {
        var result = await _researchRepository.GetPagedAsync();

        var mappedResult = result.Select(research => new ResearchDTO
        {
            Id = research.Id,
            Title = research.Title,
            Description = research.Description,
            EndDate = research.EndDate,
            Status = research.Status,
            ResearchNode = research.ResearchNode != null ? MapToNodeDTO(research.ResearchNode) : null!
        }).ToList();

        return mappedResult;
    }

    // Group 1: Core CRUD

    public async Task<ResearchDetailDTO?> GetByIdDetailAsync(Guid id)
    {
        var research = await _researchRepository.GetByIdWithCountsAsync(id);
        if (research == null) return null;

        return MapToDetailDTO(research);
    }

    public async Task<ResearchDetailDTO?> UpdateResearchAsync(Guid id, UpdateResearchPayload payload)
    {
        var research = await _researchRepository.GetByIdAsync(id);
        if (research == null) return null;

        // Validation
        if (payload.Title != null)
        {
            if (string.IsNullOrWhiteSpace(payload.Title) || payload.Title.Length > 500)
                throw new Exception("Title must not be empty and must be at most 500 characters");
            research.Title = payload.Title;
        }

        if (payload.Description != null)
        {
            if (string.IsNullOrWhiteSpace(payload.Description))
                throw new Exception("Description must not be empty");
            research.Description = payload.Description;
        }

        if (payload.Status != null)
        {
            if (!ValidStatuses.Contains(payload.Status))
                throw new Exception($"Status must be one of: {string.Join(", ", ValidStatuses)}");
            research.Status = payload.Status;
        }

        if (payload.EndDate != null)
        {
            if (payload.EndDate < research.StartDate)
                throw new Exception("EndDate must be >= StartDate");
            research.EndDate = payload.EndDate;
        }

        research.UpdatedAt = DateTime.UtcNow;
        await _researchRepository.UpdateAsync(research);

        // Reload with counts for response
        var updated = await _researchRepository.GetByIdWithCountsAsync(id);
        return MapToDetailDTO(updated!);
    }

    public async Task<bool> DeleteResearchAsync(Guid id)
    {
        return await _researchRepository.DeleteAsync(id);
    }

    public async Task<List<ResearchDTO>> GetByStatusPagedAsync(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new Exception($"Status must be one of: {string.Join(", ", ValidStatuses)}");

        var result = await _researchRepository.GetByStatusPagedAsync(status);

        return result.Select(r => new ResearchDTO
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            EndDate = r.EndDate,
            Status = r.Status,
            ResearchNode = r.ResearchNode != null ? MapToNodeDTO(r.ResearchNode) : null!
        }).ToList();
    }

    public async Task<List<ResearchDTO>> GetActiveResearchPagedAsync()
    {
        var result = await _researchRepository.GetActiveResearchPagedAsync();

        return result.Select(r => new ResearchDTO
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            EndDate = r.EndDate,
            Status = r.Status,
            ResearchNode = r.ResearchNode != null ? MapToNodeDTO(r.ResearchNode) : null!
        }).ToList();
    }

    // Group 2: Research Researchers

    public async Task<List<ResearchResearcherDTO>> GetResearchResearchersAsync(Guid researchId)
    {
        var items = await _researchRepository.GetResearchersByResearchIdAsync(researchId);
        return items.Select(MapToResearchResearcherDTO).ToList();
    }

    public async Task<ResearchResearcherDTO> AddResearchResearcherAsync(Guid researchId, AddResearchResearcherPayload payload)
    {
        if (payload.ResearcherId == Guid.Empty)
            throw new Exception("ResearcherId is required");

        // Check researcher exists
        var researcher = await _researcherRepository.GetByIdAsync(payload.ResearcherId);
        if (researcher == null)
            throw new KeyNotFoundException("ERR_RESEARCHER_NOT_FOUND");

        // MUST FIX #1: Check if a soft-deleted record exists (reactivation pattern)
        var existing = await _researchRepository.GetResearchResearcherIncludingRemovedAsync(researchId, payload.ResearcherId);

        if (existing != null)
        {
            if (existing.RemovedAt == null)
                throw new InvalidOperationException("ERR_RESEARCHER_ALREADY_ASSIGNED");

            // Reactivate the existing record
            existing.RemovedAt = null;
            existing.IsPrincipal = payload.IsPrincipal;
            existing.AssignedAt = DateTime.UtcNow;

            // Handle principal transfer
            if (payload.IsPrincipal)
                await DemoteExistingPrincipal(researchId, payload.ResearcherId);

            return MapToResearchResearcherDTO(await _researchRepository.UpdateResearchResearcherAsync(existing));
        }

        // Handle principal transfer
        if (payload.IsPrincipal)
            await DemoteExistingPrincipal(researchId, Guid.Empty);

        var entity = new ResearchResearcher
        {
            ResearchId = researchId,
            ResearcherId = payload.ResearcherId,
            IsPrincipal = payload.IsPrincipal,
            AssignedAt = DateTime.UtcNow,
            RemovedAt = null
        };

        return MapToResearchResearcherDTO(await _researchRepository.AddResearchResearcherAsync(entity));
    }

    public async Task<ResearchResearcherDTO?> UpdateResearchResearcherAsync(Guid researchId, Guid researcherId, UpdateResearchResearcherPayload payload)
    {
        var entity = await _researchRepository.GetResearchResearcherAsync(researchId, researcherId);
        if (entity == null) return null;

        if (payload.IsPrincipal.HasValue)
        {
            if (payload.IsPrincipal.Value)
                await DemoteExistingPrincipal(researchId, researcherId);

            entity.IsPrincipal = payload.IsPrincipal.Value;
        }

        return MapToResearchResearcherDTO(await _researchRepository.UpdateResearchResearcherAsync(entity));
    }

    public async Task<bool> RemoveResearchResearcherAsync(Guid researchId, Guid researcherId)
    {
        return await _researchRepository.RemoveResearchResearcherAsync(researchId, researcherId);
    }

    // Group 3: Research Volunteers

    public async Task<List<ResearchVolunteerDTO>> GetResearchVolunteersAsync(Guid researchId)
    {
        var items = await _researchRepository.GetVolunteersByResearchIdAsync(researchId);
        return items.Select(MapToResearchVolunteerDTO).ToList();
    }

    public async Task<ResearchVolunteerDTO> AddResearchVolunteerAsync(Guid researchId, AddResearchVolunteerPayload payload)
    {
        if (payload.VolunteerId == Guid.Empty)
            throw new Exception("VolunteerId is required");

        if (payload.ConsentDate > DateTime.UtcNow)
            throw new Exception("ConsentDate must not be in the future");

        if (string.IsNullOrWhiteSpace(payload.ConsentVersion))
            throw new Exception("ConsentVersion is required");

        // Check volunteer exists
        var volunteer = await _volunteerRepository.GetByIdAsync(payload.VolunteerId);
        if (volunteer == null)
            throw new KeyNotFoundException("ERR_VOLUNTEER_NOT_FOUND");

        // MUST FIX #1: Check if a soft-deleted record exists (reactivation pattern)
        var existing = await _researchRepository.GetResearchVolunteerIncludingWithdrawnAsync(researchId, payload.VolunteerId);

        if (existing != null)
        {
            if (existing.WithdrawnAt == null)
                throw new InvalidOperationException("ERR_VOLUNTEER_ALREADY_ENROLLED");

            // Reactivate the existing record
            existing.WithdrawnAt = null;
            existing.EnrollmentStatus = "Enrolled";
            existing.ConsentDate = payload.ConsentDate;
            existing.ConsentVersion = payload.ConsentVersion;
            existing.ExclusionReason = null;
            existing.EnrolledAt = DateTime.UtcNow;

            return MapToResearchVolunteerDTO(await _researchRepository.UpdateResearchVolunteerAsync(existing));
        }

        var entity = new ResearchVolunteer
        {
            ResearchId = researchId,
            VolunteerId = payload.VolunteerId,
            EnrollmentStatus = "Enrolled",
            ConsentDate = payload.ConsentDate,
            ConsentVersion = payload.ConsentVersion,
            ExclusionReason = null,
            EnrolledAt = DateTime.UtcNow,
            WithdrawnAt = null
        };

        return MapToResearchVolunteerDTO(await _researchRepository.AddResearchVolunteerAsync(entity));
    }

    public async Task<ResearchVolunteerDTO?> UpdateResearchVolunteerAsync(Guid researchId, Guid volunteerId, UpdateResearchVolunteerPayload payload)
    {
        var entity = await _researchRepository.GetResearchVolunteerAsync(researchId, volunteerId);
        if (entity == null) return null;

        if (payload.EnrollmentStatus != null)
        {
            if (!ValidEnrollmentStatuses.Contains(payload.EnrollmentStatus))
                throw new Exception($"EnrollmentStatus must be one of: {string.Join(", ", ValidEnrollmentStatuses)}");

            entity.EnrollmentStatus = payload.EnrollmentStatus;

            // Side effect: set WithdrawnAt when status changes to Withdrawn or Excluded
            if (payload.EnrollmentStatus is "Withdrawn" or "Excluded")
                entity.WithdrawnAt = DateTime.UtcNow;
        }

        if (payload.ConsentDate.HasValue)
        {
            if (payload.ConsentDate.Value > DateTime.UtcNow)
                throw new Exception("ConsentDate must not be in the future");
            entity.ConsentDate = payload.ConsentDate.Value;
        }

        if (payload.ConsentVersion != null)
            entity.ConsentVersion = payload.ConsentVersion;

        if (payload.ExclusionReason != null)
            entity.ExclusionReason = payload.ExclusionReason;

        return MapToResearchVolunteerDTO(await _researchRepository.UpdateResearchVolunteerAsync(entity));
    }

    public async Task<bool> RemoveResearchVolunteerAsync(Guid researchId, Guid volunteerId)
    {
        return await _researchRepository.RemoveResearchVolunteerAsync(researchId, volunteerId);
    }

    // Group 4: Research Applications

    public async Task<List<ApplicationDTO>> GetResearchApplicationsAsync(Guid researchId)
    {
        var items = await _researchRepository.GetApplicationsByResearchIdAsync(researchId);
        return items.Select(MapToApplicationDTO).ToList();
    }

    public async Task<ApplicationDTO> AddApplicationAsync(Guid researchId, AddApplicationPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.AppName) || payload.AppName.Length > 200)
            throw new Exception("AppName is required and must be at most 200 characters");

        if (string.IsNullOrWhiteSpace(payload.Url))
            throw new Exception("Url is required");

        if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out _))
            throw new Exception("Url must be a valid URL format");

        var entity = new Domain.Entities.Application.Application
        {
            ApplicationId = Guid.NewGuid(),
            ResearchId = researchId,
            AppName = payload.AppName,
            Url = payload.Url,
            Description = payload.Description ?? string.Empty,
            AdditionalInfo = payload.AdditionalInfo ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return MapToApplicationDTO(await _researchRepository.AddApplicationAsync(entity));
    }

    public async Task<ApplicationDTO?> UpdateApplicationAsync(Guid researchId, Guid applicationId, UpdateApplicationPayload payload)
    {
        var entity = await _researchRepository.GetApplicationByIdAndResearchIdAsync(applicationId, researchId);
        if (entity == null) return null;

        if (payload.AppName != null)
        {
            if (string.IsNullOrWhiteSpace(payload.AppName) || payload.AppName.Length > 200)
                throw new Exception("AppName must not be empty and must be at most 200 characters");
            entity.AppName = payload.AppName;
        }

        if (payload.Url != null)
        {
            if (string.IsNullOrWhiteSpace(payload.Url) || !Uri.TryCreate(payload.Url, UriKind.Absolute, out _))
                throw new Exception("Url must be a valid URL format");
            entity.Url = payload.Url;
        }

        if (payload.Description != null)
            entity.Description = payload.Description;

        if (payload.AdditionalInfo != null)
            entity.AdditionalInfo = payload.AdditionalInfo;

        entity.UpdatedAt = DateTime.UtcNow;

        return MapToApplicationDTO(await _researchRepository.UpdateApplicationAsync(entity));
    }

    public async Task<bool> DeleteApplicationAsync(Guid researchId, Guid applicationId)
    {
        return await _researchRepository.DeleteApplicationAsync(applicationId, researchId);
    }

    // Group 5: Research Devices

    public async Task<List<ResearchDeviceDTO>> GetResearchDevicesAsync(Guid researchId)
    {
        var items = await _researchRepository.GetDevicesByResearchIdAsync(researchId);
        return items.Select(MapToResearchDeviceDTO).ToList();
    }

    public async Task<ResearchDeviceDTO> AddResearchDeviceAsync(Guid researchId, AddResearchDevicePayload payload)
    {
        if (payload.DeviceId == Guid.Empty)
            throw new Exception("DeviceId is required");

        if (string.IsNullOrWhiteSpace(payload.Role) || payload.Role.Length > 200)
            throw new Exception("Role is required and must be at most 200 characters");

        if (!ValidCalibrationStatuses.Contains(payload.CalibrationStatus))
            throw new Exception($"CalibrationStatus must be one of: {string.Join(", ", ValidCalibrationStatuses)}");

        if (payload.LastCalibrationDate.HasValue && payload.LastCalibrationDate.Value > DateTime.UtcNow)
            throw new Exception("LastCalibrationDate must not be in the future");

        // Check device exists
        var device = await _deviceRepository.GetByIdAsync(payload.DeviceId);
        if (device == null)
            throw new KeyNotFoundException("ERR_DEVICE_NOT_FOUND");

        // MUST FIX #1: Check if a soft-deleted record exists (reactivation pattern)
        var existing = await _researchRepository.GetResearchDeviceIncludingRemovedAsync(researchId, payload.DeviceId);

        if (existing != null)
        {
            if (existing.RemovedAt == null)
                throw new InvalidOperationException("ERR_DEVICE_ALREADY_ASSIGNED");

            // Reactivate the existing record
            existing.RemovedAt = null;
            existing.Role = payload.Role;
            existing.CalibrationStatus = payload.CalibrationStatus;
            existing.LastCalibrationDate = payload.LastCalibrationDate;
            existing.AddedAt = DateTime.UtcNow;

            return MapToResearchDeviceDTO(await _researchRepository.UpdateResearchDeviceAsync(existing));
        }

        var entity = new Domain.Entities.Device.ResearchDevice
        {
            ResearchId = researchId,
            DeviceId = payload.DeviceId,
            Role = payload.Role,
            CalibrationStatus = payload.CalibrationStatus,
            LastCalibrationDate = payload.LastCalibrationDate,
            AddedAt = DateTime.UtcNow,
            RemovedAt = null
        };

        return MapToResearchDeviceDTO(await _researchRepository.AddResearchDeviceAsync(entity));
    }

    public async Task<ResearchDeviceDTO?> UpdateResearchDeviceAsync(Guid researchId, Guid deviceId, UpdateResearchDevicePayload payload)
    {
        var entity = await _researchRepository.GetResearchDeviceAsync(researchId, deviceId);
        if (entity == null) return null;

        if (payload.Role != null)
        {
            if (string.IsNullOrWhiteSpace(payload.Role) || payload.Role.Length > 200)
                throw new Exception("Role must not be empty and must be at most 200 characters");
            entity.Role = payload.Role;
        }

        if (payload.CalibrationStatus != null)
        {
            if (!ValidCalibrationStatuses.Contains(payload.CalibrationStatus))
                throw new Exception($"CalibrationStatus must be one of: {string.Join(", ", ValidCalibrationStatuses)}");
            entity.CalibrationStatus = payload.CalibrationStatus;
        }

        if (payload.LastCalibrationDate.HasValue)
        {
            if (payload.LastCalibrationDate.Value > DateTime.UtcNow)
                throw new Exception("LastCalibrationDate must not be in the future");
            entity.LastCalibrationDate = payload.LastCalibrationDate;
        }

        return MapToResearchDeviceDTO(await _researchRepository.UpdateResearchDeviceAsync(entity));
    }

    public async Task<bool> RemoveResearchDeviceAsync(Guid researchId, Guid deviceId)
    {
        return await _researchRepository.RemoveResearchDeviceAsync(researchId, deviceId);
    }

    // Group 6: Device Sensors

    public async Task<List<SensorDTO>> GetDeviceSensorsForResearchAsync(Guid researchId, Guid deviceId)
    {
        var sensors = await _researchRepository.GetSensorsByResearchDeviceAsync(researchId, deviceId);
        if (sensors == null) return null!;

        return sensors.Select(s => new SensorDTO
        {
            SensorId = s.SensorId,
            DeviceId = s.DeviceId,
            SensorName = s.SensorName,
            MaxSamplingRate = s.MaxSamplingRate,
            Unit = s.Unit,
            MinRange = s.MinRange,
            MaxRange = s.MaxRange,
            Accuracy = s.Accuracy,
            AdditionalInfo = s.AdditionalInfo,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }

    // Private helpers

    private void ValidateAddResearchPayload(AddResearchDTO payload)
    {
        if (string.IsNullOrEmpty(payload.Title))
            throw new Exception("Research title is required");

        if (string.IsNullOrEmpty(payload.Description))
            throw new Exception("Research description is required");

        if (payload.ResearchNodeId == Guid.Empty)
            throw new Exception("Research node ID is required");

        var node = _nodeRepository.GetByIdAsync(payload.ResearchNodeId).Result;
        if (node == null)
            throw new Exception("Research node does not exist");
    }

    private async Task DemoteExistingPrincipal(Guid researchId, Guid excludeResearcherId)
    {
        // Get all active researchers for this research
        var researchers = await _researchRepository.GetResearchersByResearchIdAsync(researchId);
        var currentPrincipal = researchers.FirstOrDefault(rr => rr.IsPrincipal && rr.ResearcherId != excludeResearcherId);

        if (currentPrincipal != null)
        {
            currentPrincipal.IsPrincipal = false;
            await _researchRepository.UpdateResearchResearcherAsync(currentPrincipal);
        }
    }

    private static ResearchDetailDTO MapToDetailDTO(Domain.Entities.Research.Research research)
    {
        return new ResearchDetailDTO
        {
            Id = research.Id,
            ResearchNodeId = research.ResearchNodeId,
            Title = research.Title,
            Description = research.Description,
            StartDate = research.StartDate,
            EndDate = research.EndDate,
            Status = research.Status,
            CreatedAt = research.CreatedAt,
            UpdatedAt = research.UpdatedAt,
            ResearchNode = research.ResearchNode != null ? MapToNodeDTO(research.ResearchNode) : null!,
            ResearcherCount = research.ResearchResearchers?.Count(rr => rr.RemovedAt == null) ?? 0,
            VolunteerCount = research.ResearchVolunteers?.Count(rv => rv.WithdrawnAt == null) ?? 0,
            ApplicationCount = research.Applications?.Count ?? 0,
            DeviceCount = research.ResearchDevices?.Count(rd => rd.RemovedAt == null) ?? 0
        };
    }

    private static ResearchNodeConnectionDTO MapToNodeDTO(Domain.Entities.Node.ResearchNode node)
    {
        return new ResearchNodeConnectionDTO
        {
            Id = node.Id,
            NodeName = node.NodeName,
            NodeUrl = node.NodeUrl,
            Status = node.Status,
            NodeAccessLevel = node.NodeAccessLevel,
            RegisteredAt = node.RegisteredAt,
            UpdatedAt = node.UpdatedAt
        };
    }

    private static ResearchResearcherDTO MapToResearchResearcherDTO(ResearchResearcher rr)
    {
        return new ResearchResearcherDTO
        {
            ResearchId = rr.ResearchId,
            ResearcherId = rr.ResearcherId,
            ResearcherName = rr.Researcher?.Name ?? string.Empty,
            Email = rr.Researcher?.Email ?? string.Empty,
            Institution = rr.Researcher?.Institution ?? string.Empty,
            Role = rr.Researcher?.Role ?? string.Empty,
            Orcid = rr.Researcher?.Orcid ?? string.Empty,
            IsPrincipal = rr.IsPrincipal,
            AssignedAt = rr.AssignedAt,
            RemovedAt = rr.RemovedAt
        };
    }

    private static ResearchVolunteerDTO MapToResearchVolunteerDTO(ResearchVolunteer rv)
    {
        return new ResearchVolunteerDTO
        {
            ResearchId = rv.ResearchId,
            VolunteerId = rv.VolunteerId,
            VolunteerName = rv.Volunteer?.Name ?? string.Empty,
            VolunteerCode = rv.Volunteer?.VolunteerCode ?? string.Empty,
            Email = rv.Volunteer?.Email ?? string.Empty,
            EnrollmentStatus = rv.EnrollmentStatus,
            ConsentDate = rv.ConsentDate,
            ConsentVersion = rv.ConsentVersion,
            ExclusionReason = rv.ExclusionReason,
            EnrolledAt = rv.EnrolledAt,
            WithdrawnAt = rv.WithdrawnAt
        };
    }

    private static ApplicationDTO MapToApplicationDTO(Domain.Entities.Application.Application app)
    {
        return new ApplicationDTO
        {
            ApplicationId = app.ApplicationId,
            ResearchId = app.ResearchId,
            AppName = app.AppName,
            Url = app.Url,
            Description = app.Description,
            AdditionalInfo = app.AdditionalInfo,
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt
        };
    }

    private static ResearchDeviceDTO MapToResearchDeviceDTO(Domain.Entities.Device.ResearchDevice rd)
    {
        return new ResearchDeviceDTO
        {
            ResearchId = rd.ResearchId,
            DeviceId = rd.DeviceId,
            DeviceName = rd.Device?.DeviceName ?? string.Empty,
            Manufacturer = rd.Device?.Manufacturer ?? string.Empty,
            Model = rd.Device?.Model ?? string.Empty,
            Role = rd.Role,
            CalibrationStatus = rd.CalibrationStatus,
            LastCalibrationDate = rd.LastCalibrationDate,
            AddedAt = rd.AddedAt,
            RemovedAt = rd.RemovedAt,
            SensorCount = rd.Device?.Sensors?.Count ?? 0
        };
    }
}
