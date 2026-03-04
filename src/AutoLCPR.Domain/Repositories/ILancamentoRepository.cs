using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Domain.Repositories;

public interface ILancamentoRepository : IRepository<Lancamento>
{
    Task<IReadOnlyList<ResumoMensalFinanceiro>> ObterResumoMensalAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default);
    Task<decimal> ObterTotalPorTipoAsync(DateTime dataInicio, DateTime dataFim, TipoLancamento tipo, int? produtorId = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Lancamento> StreamPorTipoAsync(DateTime dataInicio, DateTime dataFim, TipoLancamento tipo, int? produtorId = null, CancellationToken cancellationToken = default);
    Task<decimal> ObterTotalFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId = null, string? clienteFornecedorFiltro = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Lancamento> StreamFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId = null, string? clienteFornecedorFiltro = null, CancellationToken cancellationToken = default);
}
