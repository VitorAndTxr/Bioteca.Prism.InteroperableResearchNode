using Bioteca.Prism.Domain.Entities.Sensor;

namespace Bioteca.Prism.Service.Services.Sensor;

/// <summary>
/// Service interface for sensor operations
/// </summary>
public interface ISensorService : IService<Domain.Entities.Sensor.Sensor, Guid>
{
    /// <summary>
    /// Get sensors by device ID
    /// </summary>
    Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
