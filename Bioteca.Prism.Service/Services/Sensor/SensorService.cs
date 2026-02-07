using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Sensor;
using Bioteca.Prism.Domain.DTOs.Sensor;
using Bioteca.Prism.Domain.Payloads.Sensor;
using Bioteca.Prism.Service.Interfaces.Sensor;

namespace Bioteca.Prism.Service.Services.Sensor;

/// <summary>
/// Service implementation for sensor operations
/// </summary>
public class SensorService : BaseService<Domain.Entities.Sensor.Sensor, Guid>, ISensorService
{
    private readonly ISensorRepository _sensorRepository;

    public SensorService(ISensorRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _sensorRepository = repository;
    }

    public async Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _sensorRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
    }

    public async Task<SensorDTO> AddSensorAsync(AddSensorPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SensorName))
            throw new Exception("SensorName is required");

        if (string.IsNullOrWhiteSpace(payload.Unit))
            throw new Exception("Unit is required");

        if (payload.DeviceId == Guid.Empty)
            throw new Exception("DeviceId is required");

        var sensor = new Domain.Entities.Sensor.Sensor
        {
            SensorId = Guid.NewGuid(),
            DeviceId = payload.DeviceId,
            SensorName = payload.SensorName,
            MaxSamplingRate = payload.MaxSamplingRate,
            Unit = payload.Unit,
            MinRange = payload.MinRange,
            MaxRange = payload.MaxRange,
            Accuracy = payload.Accuracy,
            AdditionalInfo = payload.AdditionalInfo ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _sensorRepository.AddAsync(sensor);

        return new SensorDTO
        {
            SensorId = created.SensorId,
            DeviceId = created.DeviceId,
            SensorName = created.SensorName,
            MaxSamplingRate = created.MaxSamplingRate,
            Unit = created.Unit,
            MinRange = created.MinRange,
            MaxRange = created.MaxRange,
            Accuracy = created.Accuracy,
            AdditionalInfo = created.AdditionalInfo,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }
}
