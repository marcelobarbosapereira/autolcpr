using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Domain.Repositories;

public interface IChaveNFeRepository : IRepository<ChaveNFe>
{
    Task<IEnumerable<ChaveNFe>> GetByProdutorIdAsync(int produtorId);
    Task AddRangeAsync(IEnumerable<ChaveNFe> entities);
}
