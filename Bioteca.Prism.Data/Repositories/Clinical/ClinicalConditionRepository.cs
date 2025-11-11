using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Clinical;

/// <summary>
/// Repository implementation for clinical condition operations
/// </summary>
public class ClinicalConditionRepository : BaseRepository<ClinicalCondition, string>, IClinicalConditionRepository
{
    public ClinicalConditionRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<ClinicalCondition>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cc => cc.IsActive)
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<ClinicalCondition>> GetPagedAsync()
    {
        // Set request pagination in ApiContext
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        // Validate and normalize pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit

        // Build base query
        var query = _dbSet.AsQueryable();

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

        return items;
    }
}
