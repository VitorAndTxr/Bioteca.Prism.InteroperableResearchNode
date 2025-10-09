using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Data.Interfaces.Sensor;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Sensor;

/// <summary>
/// Repository implementation for sensor persistence operations
/// </summary>
public class SensorRepository : BaseRepository<Domain.Entities.Sensor.Sensor, Guid>, ISensorRepository
{
    public SensorRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
    }
}
