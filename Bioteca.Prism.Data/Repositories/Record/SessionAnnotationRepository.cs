using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for session annotation persistence operations
/// </summary>
public class SessionAnnotationRepository : BaseRepository<SessionAnnotation, Guid>, ISessionAnnotationRepository
{
    public SessionAnnotationRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<SessionAnnotation>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.RecordSessionId == sessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
