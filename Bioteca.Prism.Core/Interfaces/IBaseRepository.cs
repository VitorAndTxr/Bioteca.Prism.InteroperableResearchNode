using Bioteca.Prism.Domain.DTOs.Paging;

namespace Bioteca.Prism.Core.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD operations
/// </summary>
public interface IBaseRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get an entity by its primary key
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<List<TEntity>> GetAllAsync();

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Delete an entity by primary key
    /// </summary>
    Task<bool> DeleteAsync(TKey id);

    /// <summary>
    /// Check if an entity exists by primary key
    /// </summary>
    Task<bool> ExistsAsync(TKey id);

    /// <summary>
    /// Get a paginated list of entities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the list of items and total count</returns>
    Task<List<TEntity>> GetPagedAsync();
}
