namespace AutoLCPR.Domain.Repositories;

using Common;

/// <summary>
/// Interface genérica para repositórios
/// </summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
