using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Device;

/// <summary>
/// Repository implementation for device persistence operations
/// </summary>
public class DeviceRepository : BaseRepository<Domain.Entities.Device.Device, Guid>, IDeviceRepository
{

    private readonly IApiContext _apiContext;
    public DeviceRepository(
        PrismDbContext context,
        IApiContext apiContext
        ) : base(context, apiContext)
    {
        _apiContext = apiContext;
    }

    public async Task<List<Domain.Entities.Device.Device>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ResearchDevices.Any(rd => rd.ResearchId == researchId))
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
