using Bioteca.Prism.Core.Interfaces;

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
}
