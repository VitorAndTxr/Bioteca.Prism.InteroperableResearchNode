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

    public async Task<RecordSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rs => rs.Id == id)
            .Include(rs => rs.Records)
                .ThenInclude(r => r.RecordChannels)
            .Include(rs => rs.SessionAnnotations)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<RecordSession>> GetFilteredPagedAsync(
        Guid? researchId,
        Guid? volunteerId,
        bool? isCompleted,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _dbSet.AsQueryable();

        if (researchId.HasValue)
            query = query.Where(rs => rs.ResearchId == researchId.Value);

        if (volunteerId.HasValue)
            query = query.Where(rs => rs.VolunteerId == volunteerId.Value);

        if (isCompleted.HasValue)
            query = isCompleted.Value
                ? query.Where(rs => rs.FinishedAt != null)
                : query.Where(rs => rs.FinishedAt == null);

        if (dateFrom.HasValue)
            query = query.Where(rs => rs.StartAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(rs => rs.StartAt <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(rs => rs.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }
}
