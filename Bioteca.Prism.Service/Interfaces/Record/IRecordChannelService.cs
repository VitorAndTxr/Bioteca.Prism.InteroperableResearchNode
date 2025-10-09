using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Service.Interfaces.Record;

/// <summary>
/// Service interface for record channel operations
/// </summary>
public interface IRecordChannelService : IServiceBase<RecordChannel, Guid>
{
    /// <summary>
    /// Get record channels by record ID (with navigation properties)
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
