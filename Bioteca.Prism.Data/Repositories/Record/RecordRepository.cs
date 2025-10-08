using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for record persistence operations
/// </summary>
public class RecordRepository : Repository<Domain.Entities.Record.Record, Guid>, IRecordRepository
{
    public RecordRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<Domain.Entities.Record.Record>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.RecordSessionId == sessionId)
            .Include(r => r.RecordChannels)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Record.Record>> GetByRecordTypeAsync(string recordType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.RecordType == recordType)
            .ToListAsync(cancellationToken);
    }
}
