namespace AutoLCPR.Domain.Repositories;

using Entities;

/// <summary>
/// Interface do repositório de Produtores
/// </summary>
public interface IProdutorRepository : IRepository<Produtor>
{
    Task<Produtor?> GetByNomeAsync(string nome);
    Task<IEnumerable<Produtor>> GetComRebanhos();
    Task<IEnumerable<Produtor>> GetComNotasFiscais();
}
