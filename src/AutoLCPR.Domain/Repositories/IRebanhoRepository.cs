namespace AutoLCPR.Domain.Repositories;

using Entities;

/// <summary>
/// Interface do repositório de Rebanhos
/// </summary>
public interface IRebanhoRepository : IRepository<Rebanho>
{
    Task<IEnumerable<Rebanho>> GetByProdutorIdAsync(int produtorId);
    Task<Rebanho?> GetByIdRebanhoAsync(string idRebanho);
}
