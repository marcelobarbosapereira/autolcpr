namespace AutoLCPR.Domain.Repositories;

using Entities;

/// <summary>
/// Interface do repositório de Notas Fiscais
/// </summary>
public interface INotaFiscalRepository : IRepository<NotaFiscal>
{
    Task<IEnumerable<NotaFiscal>> GetByProdutorIdAsync(int produtorId);
    Task<NotaFiscal?> GetByChaveAcessoAsync(string chaveAcesso);
    Task<IEnumerable<NotaFiscal>> GetByTipoAsync(TipoNota tipo);
    Task<IEnumerable<NotaFiscal>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim);
}
