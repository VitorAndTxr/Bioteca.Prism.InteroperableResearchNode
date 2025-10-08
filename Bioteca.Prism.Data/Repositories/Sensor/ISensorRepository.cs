using Bioteca.Prism.Domain.Entities.Sensor;

namespace Bioteca.Prism.Data.Repositories.Sensor;

/// <summary>
/// Repository interface for sensor persistence operations
/// </summary>
public interface ISensorRepository : IRepository<Domain.Entities.Sensor.Sensor, Guid>
{
    /// <summary>
    /// Get sensors by device ID
    /// </summary>
    Task<List<Domain.Entities.Sensor.Sensor>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
