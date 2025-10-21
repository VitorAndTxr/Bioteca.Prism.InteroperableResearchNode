using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.User;

public interface IUserRepository : IBaseRepository<Domain.Entities.User.User, Guid>
{
    public Domain.Entities.User.User? GetByUsername(string username);
}