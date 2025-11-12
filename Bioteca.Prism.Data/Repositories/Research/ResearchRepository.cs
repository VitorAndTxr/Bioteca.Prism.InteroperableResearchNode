using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Research;

/// <summary>
/// Repository implementation for research persistence operations
/// </summary>
public class ResearchRepository : BaseRepository<Domain.Entities.Research.Research, Guid>, IResearchRepository
{
    public ResearchRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ResearchNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == "Active" && (r.EndDate == null || r.EndDate > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<Domain.Entities.Research.Research>> GetPagedAsync()
    {
        // Set request pagination in ApiContext
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        // Validate and normalize pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit

        // Build base query with related entities
        var query = _dbSet
            .Include(r => r.ResearchNode)
            .AsQueryable();

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }
}
