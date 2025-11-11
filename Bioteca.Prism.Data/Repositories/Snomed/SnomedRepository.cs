using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Snomed;

/// <summary>
/// Repository implementation for SNOMED CT laterality codes
/// </summary>
public class SnomedLateralityRepository : BaseRepository<SnomedLaterality, string>, ISnomedLateralityRepository
{
    public SnomedLateralityRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sl => sl.IsActive)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for SNOMED CT topographical modifier codes
/// </summary>
public class SnomedTopographicalModifierRepository : BaseRepository<SnomedTopographicalModifier, string>, ISnomedTopographicalModifierRepository
{
    public SnomedTopographicalModifierRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<SnomedTopographicalModifier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(stm => stm.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(stm => stm.Category == category && stm.IsActive)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for SNOMED CT body region codes
/// </summary>
public class SnomedBodyRegionRepository : BaseRepository<SnomedBodyRegion, string>, ISnomedBodyRegionRepository
{
    public SnomedBodyRegionRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<SnomedBodyRegion>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.ParentRegionCode == parentRegionCode && sbr.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.ParentRegionCode == null && sbr.IsActive)
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<SnomedBodyRegion>> GetPagedAsync()
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
            .Include(u => u.ParentRegion)
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

/// <summary>
/// Repository implementation for SNOMED CT body structure codes
/// </summary>
public class SnomedBodyStructureRepository : BaseRepository<SnomedBodyStructure, string>, ISnomedBodyStructureRepository
{
    public SnomedBodyStructureRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<List<SnomedBodyStructure>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.BodyRegionCode == bodyRegionCode && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.ParentStructureCode == parentStructureCode && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.StructureType == structureType && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<SnomedBodyStructure>> GetPagedAsync()
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
            .Include(u => u.ParentStructure)
            .Include(u => u.BodyRegion)
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
