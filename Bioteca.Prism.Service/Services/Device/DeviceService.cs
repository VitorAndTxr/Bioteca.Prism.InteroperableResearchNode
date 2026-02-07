using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Domain.DTOs.Device;
using Bioteca.Prism.Domain.Payloads.Device;
using Bioteca.Prism.Service.Interfaces.Device;

namespace Bioteca.Prism.Service.Services.Device;

/// <summary>
/// Service implementation for device operations
/// </summary>
public class DeviceService : BaseService<Domain.Entities.Device.Device, Guid>, IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository repository, IApiContext apiContext) : base(repository, apiContext)
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

    public async Task<List<DeviceDTO>> GetAllDevicesPaginateAsync()
    {
        var result = await _deviceRepository.GetPagedAsync();

        return result.Select(d => new DeviceDTO
        {
            DeviceId = d.DeviceId,
            DeviceName = d.DeviceName,
            Manufacturer = d.Manufacturer,
            Model = d.Model,
            AdditionalInfo = d.AdditionalInfo,
            CreatedAt = d.CreatedAt,
            SensorCount = d.Sensors?.Count ?? 0
        }).ToList();
    }

    public async Task<DeviceDTO> AddDeviceAsync(AddDevicePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.DeviceName))
            throw new Exception("DeviceName is required");

        if (string.IsNullOrWhiteSpace(payload.Manufacturer))
            throw new Exception("Manufacturer is required");

        if (string.IsNullOrWhiteSpace(payload.Model))
            throw new Exception("Model is required");

        var device = new Domain.Entities.Device.Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = payload.DeviceName,
            Manufacturer = payload.Manufacturer,
            Model = payload.Model,
            AdditionalInfo = payload.AdditionalInfo ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _deviceRepository.AddAsync(device);

        return new DeviceDTO
        {
            DeviceId = created.DeviceId,
            DeviceName = created.DeviceName,
            Manufacturer = created.Manufacturer,
            Model = created.Model,
            AdditionalInfo = created.AdditionalInfo,
            CreatedAt = created.CreatedAt,
            SensorCount = 0
        };
    }
}
