using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Clinical;

/// <summary>
/// Repository implementation for allergy/intolerance operations
/// </summary>
public class AllergyIntoleranceRepository : BaseRepository<AllergyIntolerance, string>, IAllergyIntoleranceRepository
{
    public AllergyIntoleranceRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<AllergyIntolerance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ai => ai.IsActive)
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<AllergyIntolerance>> GetPagedAsync()
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
