using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Paging;
using Bioteca.Prism.Domain.DTOs.Paging;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Core.Database;

/// <summary>
/// Generic repository implementation for basic CRUD operations
/// </summary>
public class BaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly IApiContext _apiContext;

    public BaseRepository(DbContext context, IApiContext apiContext)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _apiContext = apiContext;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }catch(Exception ex)
        {
            throw ex;
        }

    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }

    public virtual async Task<List<TEntity>> GetPagedAsync()
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

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize,totalPages);

        return items;
    }
}
