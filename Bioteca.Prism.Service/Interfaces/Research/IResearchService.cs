using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Application;
using Bioteca.Prism.Domain.DTOs.Research;
using Bioteca.Prism.Domain.DTOs.Sensor;
using Bioteca.Prism.Domain.Payloads.Application;
using Bioteca.Prism.Domain.Payloads.Research;

namespace Bioteca.Prism.Service.Interfaces.Research;

/// <summary>
/// Service interface for research project operations
/// </summary>
public interface IResearchService : IServiceBase<Domain.Entities.Research.Research, Guid>
{
    /// <summary>
    /// Get research projects by node ID
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get research projects by status
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active research projects (no end date or end date in future)
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new research project
    /// </summary>
    Task<Domain.Entities.Research.Research> AddAsync(AddResearchDTO payload);

    /// <summary>
    /// Get all research projects with pagination
    /// </summary>
    Task<List<ResearchDTO>> GetAllPaginateAsync();

    // Group 1: Core CRUD
    Task<ResearchDetailDTO?> GetByIdDetailAsync(Guid id);
    Task<ResearchDetailDTO?> UpdateResearchAsync(Guid id, UpdateResearchPayload payload);
    Task<bool> DeleteResearchAsync(Guid id);
    Task<List<ResearchDTO>> GetByStatusPagedAsync(string status);
    Task<List<ResearchDTO>> GetActiveResearchPagedAsync();

    // Group 2: Research Researchers
    Task<List<ResearchResearcherDTO>> GetResearchResearchersAsync(Guid researchId);
    Task<ResearchResearcherDTO> AddResearchResearcherAsync(Guid researchId, AddResearchResearcherPayload payload);
    Task<ResearchResearcherDTO?> UpdateResearchResearcherAsync(Guid researchId, Guid researcherId, UpdateResearchResearcherPayload payload);
    Task<bool> RemoveResearchResearcherAsync(Guid researchId, Guid researcherId);

    // Group 3: Research Volunteers
    Task<List<ResearchVolunteerDTO>> GetResearchVolunteersAsync(Guid researchId);
    Task<ResearchVolunteerDTO> AddResearchVolunteerAsync(Guid researchId, AddResearchVolunteerPayload payload);
    Task<ResearchVolunteerDTO?> UpdateResearchVolunteerAsync(Guid researchId, Guid volunteerId, UpdateResearchVolunteerPayload payload);
    Task<bool> RemoveResearchVolunteerAsync(Guid researchId, Guid volunteerId);

    // Group 4: Research Applications
    Task<List<ApplicationDTO>> GetResearchApplicationsAsync(Guid researchId);
    Task<ApplicationDTO> AddApplicationAsync(Guid researchId, AddApplicationPayload payload);
    Task<ApplicationDTO?> UpdateApplicationAsync(Guid researchId, Guid applicationId, UpdateApplicationPayload payload);
    Task<bool> DeleteApplicationAsync(Guid researchId, Guid applicationId);

    // Group 5: Research Devices
    Task<List<ResearchDeviceDTO>> GetResearchDevicesAsync(Guid researchId);
    Task<ResearchDeviceDTO> AddResearchDeviceAsync(Guid researchId, AddResearchDevicePayload payload);
    Task<ResearchDeviceDTO?> UpdateResearchDeviceAsync(Guid researchId, Guid deviceId, UpdateResearchDevicePayload payload);
    Task<bool> RemoveResearchDeviceAsync(Guid researchId, Guid deviceId);

    // Group 6: Device Sensors
    Task<List<SensorDTO>> GetDeviceSensorsForResearchAsync(Guid researchId, Guid deviceId);
    Task<List<SensorDTO>> GetAllSensorsForResearchAsync(Guid researchId);
}
