using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Data.Interfaces.Record;

/// <summary>
/// Repository interface for session annotation persistence operations
/// </summary>
public interface ISessionAnnotationRepository : IBaseRepository<SessionAnnotation, Guid>
{
    /// <summary>
    /// Get all annotations for a session, ordered by CreatedAt ascending
    /// </summary>
    Task<List<SessionAnnotation>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
