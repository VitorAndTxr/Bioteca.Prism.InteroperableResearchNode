using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Sensor;

/// <summary>
/// Repository interface for sensor persistence operations
/// </summary>
public interface ISensorRepository : IBaseRepository<Domain.Entities.Sensor.Sensor, Guid>
{
    /// <summary>
    /// Get sensors by device ID
    /// </summary>
    Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
