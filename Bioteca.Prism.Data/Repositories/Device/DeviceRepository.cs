using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Device;

/// <summary>
/// Repository implementation for device persistence operations
/// </summary>
public class DeviceRepository : Repository<Domain.Entities.Device.Device, Guid>, IDeviceRepository
{
    public DeviceRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<Domain.Entities.Device.Device>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ResearchId == researchId)
            .Include(d => d.Sensors)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Device.Device>> GetByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.Manufacturer == manufacturer)
            .ToListAsync(cancellationToken);
    }
}
