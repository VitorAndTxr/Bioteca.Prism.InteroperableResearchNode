using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Interfaces.Record;

/// <summary>
/// Repository interface for record session persistence operations
/// </summary>
public interface IRecordSessionRepository : IBaseRepository<RecordSession, Guid>
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

    /// <summary>
    /// Get a record session by ID with all related details (Records, RecordChannels, SessionAnnotations)
    /// </summary>
    Task<RecordSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get filtered and paginated record sessions
    /// </summary>
    Task<List<RecordSession>> GetFilteredPagedAsync(
        Guid? researchId,
        Guid? volunteerId,
        bool? isCompleted,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);
}
