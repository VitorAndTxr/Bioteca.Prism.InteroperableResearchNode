using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Sensor;
using Bioteca.Prism.Service.Interfaces.Sensor;

namespace Bioteca.Prism.Service.Services.Sensor;

/// <summary>
/// Service implementation for sensor operations
/// </summary>
public class SensorService : BaseService<Domain.Entities.Sensor.Sensor, Guid>, ISensorService
{
    private readonly ISensorRepository _sensorRepository;

    public SensorService(ISensorRepository repository) : base(repository)
    {
        _sensorRepository = repository;
    }

    public async Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _sensorRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
    }
}
