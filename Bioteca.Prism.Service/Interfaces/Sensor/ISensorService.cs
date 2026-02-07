using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Sensor;
using Bioteca.Prism.Domain.Payloads.Sensor;

namespace Bioteca.Prism.Service.Interfaces.Sensor;

/// <summary>
/// Service interface for sensor operations
/// </summary>
public interface ISensorService : IServiceBase<Domain.Entities.Sensor.Sensor, Guid>
{
    /// <summary>
    /// Get sensors by device ID
    /// </summary>
    Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new sensor from payload
    /// </summary>
    Task<SensorDTO> AddSensorAsync(AddSensorPayload payload);

    Task<SensorDTO?> UpdateSensorAsync(Guid sensorId, UpdateSensorPayload payload);

    Task<bool> DeleteSensorAsync(Guid sensorId);
}
