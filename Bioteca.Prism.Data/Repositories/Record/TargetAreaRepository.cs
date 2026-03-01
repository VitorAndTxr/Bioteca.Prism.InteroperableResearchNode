using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for target area persistence operations
/// </summary>
public class TargetAreaRepository : BaseRepository<TargetArea, Guid>, ITargetAreaRepository
{
    public TargetAreaRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<TargetArea>> GetByRecordSessionIdAsync(Guid recordSessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ta => ta.RecordSessionId == recordSessionId)
            .Include(ta => ta.BodyStructure)
            .Include(ta => ta.Laterality)
            .Include(ta => ta.TopographicalModifiers)
                .ThenInclude(tm => tm.TopographicalModifier)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ta => ta.BodyStructureCode == bodyStructureCode)
            .ToListAsync(cancellationToken);
    }
}
