using Bioteca.Prism.Domain.Entities.Volunteer;

namespace Bioteca.Prism.Data.Repositories.Volunteer;

/// <summary>
/// Repository interface for volunteer persistence operations
/// </summary>
public interface IVolunteerRepository : IRepository<Domain.Entities.Volunteer.Volunteer, Guid>
{
    /// <summary>
    /// Get volunteer by volunteer code
    /// </summary>
    Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get volunteers by node ID
    /// </summary>
    Task<List<Domain.Entities.Volunteer.Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get volunteers by consent status
    /// </summary>
    Task<List<Domain.Entities.Volunteer.Volunteer>> GetByConsentStatusAsync(string consentStatus, CancellationToken cancellationToken = default);
}
