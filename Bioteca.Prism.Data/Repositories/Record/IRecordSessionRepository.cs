using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository interface for record session persistence operations
/// </summary>
public interface IRecordSessionRepository : IRepository<RecordSession, Guid>
{
    /// <summary>
    /// Get record sessions by research ID
    /// </summary>
    Task<List<RecordSession>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get record sessions by volunteer ID
    /// </summary>
    Task<List<RecordSession>> GetByVolunteerIdAsync(Guid volunteerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active (unfinished) record sessions
    /// </summary>
    Task<List<RecordSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
}
