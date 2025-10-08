namespace Bioteca.Prism.Data.Repositories;

/// <summary>
/// Generic repository interface for basic CRUD operations
/// </summary>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get an entity by its primary key
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity by primary key
    /// </summary>
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an entity exists by primary key
    /// </summary>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}
