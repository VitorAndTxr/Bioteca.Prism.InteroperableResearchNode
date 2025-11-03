using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Researcher;

/// <summary>
/// Repository implementation for research-researcher persistence and search operations
/// </summary>
public class ResearchResearcherRepository : BaseRepository<Domain.Entities.Research.ResearchResearcher, Guid>, IResearchResearcherRepository
{
    public ResearchResearcherRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }
    public async Task<List<Domain.Entities.Research.ResearchResearcher>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rr => rr.ResearchId == researchId)
            .ToListAsync(cancellationToken);
    }
    public async Task<List<Domain.Entities.Research.ResearchResearcher>> GetByResearcherIdAsync(Guid researcherId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rr => rr.ResearcherId == researcherId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Research.ResearchResearcher?> GetByResearcherIdAndResearchIdAsync(Guid researcherId, Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rr => rr.ResearcherId == researcherId && rr.ResearchId == researchId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetResearchesFromResearcherIdAsync(Guid researcherId, CancellationToken cancellationToken = default)
    {
        var researchResearchers = await _dbSet
            .Include(rr => rr.Research)
            .Where(rr => rr.ResearcherId == researcherId)
            .ToListAsync(cancellationToken);

        var userResearchs = researchResearchers.Select(x => x.Research).ToList();

        return userResearchs;
    }
}
