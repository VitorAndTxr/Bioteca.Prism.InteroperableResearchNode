using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Data.Persistence.Contexts;

namespace Bioteca.Prism.Data.Repositories.User
{
    public class UserRepository : BaseRepository<Domain.Entities.User.User, Guid>, IUserRepository
    {
        public UserRepository(PrismDbContext context) : base(context)
        {
        }

        public Domain.Entities.User.User? GetByUsername(string username)
        {
            try
            {
                return _dbSet.FirstOrDefault(u => u.Login == username);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
