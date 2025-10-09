using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for record channel persistence operations
/// </summary>
public class RecordChannelRepository : BaseRepository<RecordChannel, Guid>, IRecordChannelRepository
{
    public RecordChannelRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<RecordChannel>> GetByRecordIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rc => rc.RecordId == recordId)
            .Include(rc => rc.TargetAreas)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RecordChannel>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rc => rc.SensorId == sensorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RecordChannel>> GetBySignalTypeAsync(string signalType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rc => rc.SignalType == signalType)
            .ToListAsync(cancellationToken);
    }
}
