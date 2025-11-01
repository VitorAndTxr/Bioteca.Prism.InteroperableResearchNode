using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Requests.Node;
using Microsoft.AspNetCore.Mvc;


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
}

