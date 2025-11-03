using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Researcher;

/// <summary>
/// Repository implementation for researcher persistence operations
/// </summary>
public class ResearcherRepository : BaseRepository<Domain.Entities.Researcher.Researcher, Guid>, IResearcherRepository
{
    public ResearcherRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<Domain.Entities.Researcher.Researcher?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.Email == email, cancellationToken);
    }

    public async Task<List<Domain.Entities.Researcher.Researcher>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ResearchNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Researcher.Researcher>> GetByInstitutionAsync(string institution, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Institution == institution)
            .ToListAsync(cancellationToken);
    }
}
