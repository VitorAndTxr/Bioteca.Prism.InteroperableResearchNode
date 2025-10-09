using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Service.Interfaces.Device;

namespace Bioteca.Prism.Service.Services.Device;

/// <summary>
/// Service implementation for device operations
/// </summary>
public class DeviceService : BaseService<Domain.Entities.Device.Device, Guid>, IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository repository) : base(repository)
    {
        _deviceRepository = repository;
    }

    public async Task<List<Domain.Entities.Device.Device>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.GetByResearchIdAsync(researchId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Device.Device>> GetByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.GetByManufacturerAsync(manufacturer, cancellationToken);
    }
}
