using Bioteca.Prism.Core.Context;
using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Application;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Application;

/// <summary>
/// Repository implementation for application persistence operations
/// </summary>
public class ApplicationRepository : BaseRepository<Domain.Entities.Application.Application, Guid>, IApplicationRepository
{
    private readonly IApiContext _apiContext;
    public ApplicationRepository(
        PrismDbContext context,
        IApiContext apiContext
        ) : base(context,apiContext)
    {
    }

    // TODO: Restore when ResearchApplication join table is implemented
    // public async Task<List<Domain.Entities.Application.Application>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    // {
    //     return await _dbSet
    //         .Where(a => a.ResearchApplications.Any(ra => ra.ResearchId == researchId))
    //         .ToListAsync(cancellationToken);
    // }
}
