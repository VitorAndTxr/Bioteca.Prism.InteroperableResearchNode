using Bioteca.Prism.Domain.DTOs.Paging;

namespace Bioteca.Prism.Core.Interfaces;

/// <summary>
/// Base service interface for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IServiceBase<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<List<TEntity>> GetAllAsync();

    /// <summary>
    /// Create new entity
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>
    /// Update existing entity
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Delete entity by ID
    /// </summary>
    Task<bool> DeleteAsync(TKey id);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(TKey id);

    /// <summary>
    /// Get paginated list of entities
    /// </summary>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="apiContext">API context to populate with pagination metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result with items and metadata</returns>
    Task<List<TEntity>> GetPagedAsync();
}
