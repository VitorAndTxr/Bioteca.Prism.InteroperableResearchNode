using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.User
{
    public class UserRepository : BaseRepository<Domain.Entities.User.User, Guid>, IUserRepository
    {
        public UserRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
        {
        }

        public Domain.Entities.User.User? GetByUsername(string username)
        {
            try
            {
                return _dbSet
                    .Include(u => u.Researcher)
                        .ThenInclude(r => r.ResearchResearchers)
                    .FirstOrDefault(u => u.Login == username);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override async Task<List<Domain.Entities.User.User>> GetPagedAsync()
        {
            // Set request pagination in ApiContext
            var page = _apiContext.PagingContext.RequestPaging.Page;
            var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

            // Validate and normalize pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size limit

            // Build base query with related entities
            var query = _dbSet
                .Include(u => u.Researcher)
                    .ThenInclude(r => r.ResearchResearchers)
                .AsQueryable();

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

            return items;
        }
    }
}
