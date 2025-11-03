using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Paging;

namespace Bioteca.Prism.Core.Service;

/// <summary>
/// Base service implementation for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public class BaseService<TEntity, TKey> : IServiceBase<TEntity, TKey> where TEntity : class
{
    protected readonly IBaseRepository<TEntity, TKey> _repository;

    protected readonly IApiContext _apiContext;

    public BaseService(IBaseRepository<TEntity, TKey> repository, IApiContext apiContext)
    {
        _repository = repository;
        _apiContext = apiContext;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        return await _repository.AddAsync(entity);
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        return await _repository.UpdateAsync(entity);
    }

    public virtual async Task<bool> DeleteAsync(TKey id)
    {
        return await _repository.DeleteAsync(id);
    }

    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        return await _repository.ExistsAsync(id);
    }

    public virtual async Task<List<TEntity>> GetPagedAsync()
    {

        // Get paginated data from repository
        var result = await _repository.GetPagedAsync();

        return result;
    }
}
