using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for target area persistence operations
/// </summary>
public class TargetAreaRepository : Repository<TargetArea, Guid>, ITargetAreaRepository
{
    public TargetAreaRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<TargetArea>> GetByRecordChannelIdAsync(Guid recordChannelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ta => ta.RecordChannelId == recordChannelId)
            .Include(ta => ta.BodyStructure)
            .Include(ta => ta.Laterality)
            .Include(ta => ta.TopographicalModifier)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ta => ta.BodyStructureCode == bodyStructureCode)
            .ToListAsync(cancellationToken);
    }
}
