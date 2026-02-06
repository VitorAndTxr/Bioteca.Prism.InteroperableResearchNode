using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Volunteer;
using Bioteca.Prism.Domain.Payloads.Volunteer;

namespace Bioteca.Prism.Service.Interfaces.Volunteer;

/// <summary>
/// Service interface for volunteer operations
/// </summary>
public interface IVolunteerService : IServiceBase<Domain.Entities.Volunteer.Volunteer, Guid>
{
    /// <summary>
    /// Get volunteers by node ID
    /// </summary>
    Task<List<Domain.Entities.Volunteer.Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get volunteer by unique code
    /// </summary>
    Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all volunteers paginated as DTOs
    /// </summary>
    Task<List<VolunteerDTO>> GetAllPaginateAsync();

    /// <summary>
    /// Get volunteer by ID as DTO
    /// </summary>
    Task<VolunteerDTO?> GetVolunteerByIdAsync(Guid id);

    /// <summary>
    /// Add a new volunteer
    /// </summary>
    Task<Domain.Entities.Volunteer.Volunteer> AddAsync(AddVolunteerPayload payload);

    /// <summary>
    /// Update an existing volunteer
    /// </summary>
    Task<VolunteerDTO?> UpdateVolunteerAsync(Guid id, UpdateVolunteerPayload payload);

    /// <summary>
    /// Delete a volunteer by ID
    /// </summary>
    Task<bool> DeleteVolunteerAsync(Guid id);
}
