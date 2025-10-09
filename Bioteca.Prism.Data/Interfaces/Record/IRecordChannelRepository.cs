using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Interfaces.Record;

/// <summary>
/// Repository interface for record channel persistence operations
/// </summary>
public interface IRecordChannelRepository : IBaseRepository<RecordChannel, Guid>
{
    /// <summary>
    /// Get record channels by record ID
    /// </summary>
    Task<List<RecordChannel>> GetByRecordIdAsync(Guid recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get record channels by sensor ID
    /// </summary>
    Task<List<RecordChannel>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get record channels by signal type
    /// </summary>
    Task<List<RecordChannel>> GetBySignalTypeAsync(string signalType, CancellationToken cancellationToken = default);
}
