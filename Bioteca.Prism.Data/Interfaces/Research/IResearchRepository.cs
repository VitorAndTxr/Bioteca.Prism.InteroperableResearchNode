using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Application;
using Bioteca.Prism.Domain.Entities.Device;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Sensor;

namespace Bioteca.Prism.Data.Interfaces.Research;

/// <summary>
/// Repository interface for research persistence operations
/// </summary>
public interface IResearchRepository : IBaseRepository<Domain.Entities.Research.Research, Guid>
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
    /// Get active research projects
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default);

    // Group 1: Core CRUD additions
    Task<Domain.Entities.Research.Research?> GetByIdWithCountsAsync(Guid id);
    Task<List<Domain.Entities.Research.Research>> GetByStatusPagedAsync(string status);
    Task<List<Domain.Entities.Research.Research>> GetActiveResearchPagedAsync();

    // Group 2: ResearchResearcher junction operations
    Task<List<ResearchResearcher>> GetResearchersByResearchIdAsync(Guid researchId);
    Task<ResearchResearcher?> GetResearchResearcherAsync(Guid researchId, Guid researcherId);
    Task<ResearchResearcher?> GetResearchResearcherIncludingRemovedAsync(Guid researchId, Guid researcherId);
    Task<ResearchResearcher> AddResearchResearcherAsync(ResearchResearcher entity);
    Task<ResearchResearcher> UpdateResearchResearcherAsync(ResearchResearcher entity);
    Task<bool> RemoveResearchResearcherAsync(Guid researchId, Guid researcherId);

    // Group 3: ResearchVolunteer junction operations
    Task<List<ResearchVolunteer>> GetVolunteersByResearchIdAsync(Guid researchId);
    Task<ResearchVolunteer?> GetResearchVolunteerAsync(Guid researchId, Guid volunteerId);
    Task<ResearchVolunteer?> GetResearchVolunteerIncludingWithdrawnAsync(Guid researchId, Guid volunteerId);
    Task<ResearchVolunteer> AddResearchVolunteerAsync(ResearchVolunteer entity);
    Task<ResearchVolunteer> UpdateResearchVolunteerAsync(ResearchVolunteer entity);
    Task<bool> RemoveResearchVolunteerAsync(Guid researchId, Guid volunteerId);

    // Group 4: Application operations (scoped to research)
    Task<List<Bioteca.Prism.Domain.Entities.Application.Application>> GetApplicationsByResearchIdAsync(Guid researchId);
    Task<Domain.Entities.Application.Application?> GetApplicationByIdAndResearchIdAsync(Guid applicationId, Guid researchId);
    Task<Domain.Entities.Application.Application> AddApplicationAsync(Domain.Entities.Application.Application entity);
    Task<Domain.Entities.Application.Application> UpdateApplicationAsync(Domain.Entities.Application.Application entity);
    Task<bool> DeleteApplicationAsync(Guid applicationId, Guid researchId);

    // Group 5: ResearchDevice junction operations
    Task<List<ResearchDevice>> GetDevicesByResearchIdAsync(Guid researchId);
    Task<ResearchDevice?> GetResearchDeviceAsync(Guid researchId, Guid deviceId);
    Task<ResearchDevice?> GetResearchDeviceIncludingRemovedAsync(Guid researchId, Guid deviceId);
    Task<ResearchDevice> AddResearchDeviceAsync(ResearchDevice entity);
    Task<ResearchDevice> UpdateResearchDeviceAsync(ResearchDevice entity);
    Task<bool> RemoveResearchDeviceAsync(Guid researchId, Guid deviceId);

    // Group 6: Sensor read (scoped to research-device)
    Task<List<Domain.Entities.Sensor.Sensor>> GetSensorsByResearchDeviceAsync(Guid researchId, Guid deviceId);
    Task<List<Domain.Entities.Sensor.Sensor>> GetAllSensorsByResearchIdAsync(Guid researchId);
}
