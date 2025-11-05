using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Researcher;
using Bioteca.Prism.Domain.Payloads.User;

namespace Bioteca.Prism.Service.Interfaces.Researcher;

/// <summary>
/// Service interface for researcher operations
/// </summary>
public interface IResearcherService : IServiceBase<Domain.Entities.Researcher.Researcher, Guid>
{
    /// <summary>
    /// Get researchers by node ID
    /// </summary>
    Task<List<ResearcherDTO>> GetByNodeIdAsync(Guid nodeId);

    /// <summary>
    /// Get researchers by institution
    /// </summary>
    Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution);

    /// <summary>
    /// Add a new user with encrypted password
    /// </summary>
    Task<Domain.Entities.Researcher.Researcher?> AddAsync(AddResearcherPayload payload);

    /// <summary>
    /// Get all users paginated
    /// </summary>
    Task<List<ResearcherDTO>> GetAllResearchersPaginateAsync();

}
