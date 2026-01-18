using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.User;

public interface IUserRepository : IBaseRepository<Domain.Entities.User.User, Guid>
{
    public Domain.Entities.User.User? GetByUsername(string username);

    /// <summary>
    /// Get user by ID with Researcher navigation property included
    /// </summary>
    Task<Domain.Entities.User.User?> GetByIdWithResearcherAsync(Guid id);
}