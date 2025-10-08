using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Application;

/// <summary>
/// Repository implementation for application persistence operations
/// </summary>
public class ApplicationRepository : Repository<Domain.Entities.Application.Application, Guid>, IApplicationRepository
{
    public ApplicationRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.ResearchApplications.Any(ra => ra.ResearchId == researchId))
            .ToListAsync(cancellationToken);
    }
}
