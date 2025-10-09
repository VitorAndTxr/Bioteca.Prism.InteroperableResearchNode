using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Record;

/// <summary>
/// Repository interface for record persistence operations
/// </summary>
public interface IRecordRepository : IBaseRepository<Domain.Entities.Record.Record, Guid>
{
    /// <summary>
    /// Get records by session ID
    /// </summary>
    Task<List<Domain.Entities.Record.Record>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get records by type
    /// </summary>
    Task<List<Domain.Entities.Record.Record>> GetByRecordTypeAsync(string recordType, CancellationToken cancellationToken = default);
}
