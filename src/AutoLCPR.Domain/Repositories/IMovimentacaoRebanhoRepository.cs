using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Domain.Repositories;

public interface IMovimentacaoRebanhoRepository : IRepository<MovimentacaoRebanho>
{
    Task<IReadOnlyList<ResumoMovimentacaoRebanho>> ObterResumoPorTipoAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default);
    Task<ResumoConsolidadoRebanho> ObterResumoConsolidadoAnualAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MovimentacaoRebanho> StreamPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default);
}
