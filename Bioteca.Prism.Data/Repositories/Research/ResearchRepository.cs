using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Research;

/// <summary>
/// Repository implementation for research persistence operations
/// </summary>
public class ResearchRepository : BaseRepository<Domain.Entities.Research.Research, Guid>, IResearchRepository
{
    public ResearchRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ResearchNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == "Active" && (r.EndDate == null || r.EndDate > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }
}
