using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Record;

/// <summary>
/// Repository implementation for record session persistence operations
/// </summary>
public class RecordSessionRepository : BaseRepository<RecordSession, Guid>, IRecordSessionRepository
{
    public RecordSessionRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<RecordSession>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rs => rs.ResearchId == researchId)
            .Include(rs => rs.Records)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RecordSession>> GetByVolunteerIdAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rs => rs.VolunteerId == volunteerId)
            .Include(rs => rs.Records)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RecordSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rs => rs.FinishedAt == null)
            .ToListAsync(cancellationToken);
    }
}
