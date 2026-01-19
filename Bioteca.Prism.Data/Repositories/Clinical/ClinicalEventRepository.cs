using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Clinical;

/// <summary>
/// Repository implementation for clinical event operations
/// </summary>
public class ClinicalEventRepository : BaseRepository<ClinicalEvent, string>, IClinicalEventRepository
{
    public ClinicalEventRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<ClinicalEvent>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ce => ce.IsActive)
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<ClinicalEvent>> GetPagedAsync()
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _dbSet.AsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }
}
