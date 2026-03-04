using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace AutoLCPR.Infrastructure.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly AppDbContext _dbContext;

    public LancamentoRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Lancamento?> GetByIdAsync(int id)
    {
        return await _dbContext.Lancamentos.FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<Lancamento>> GetAllAsync()
    {
        return await _dbContext.Lancamentos
            .AsNoTracking()
            .OrderByDescending(item => item.Data)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Lancamento entity)
    {
        await _dbContext.Lancamentos.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(Lancamento entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.Lancamentos.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Lancamentos.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.Lancamentos.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ResumoMensalFinanceiro>> ObterResumoMensalAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        var receitaPorMes = await ObterTotaisMensaisPorTipoAsync(dataInicio, dataFim, TipoLancamento.Receita, produtorId, cancellationToken);
        var despesaPorMes = await ObterTotaisMensaisPorTipoAsync(dataInicio, dataFim, TipoLancamento.Despesa, produtorId, cancellationToken);

        var resultado = Enumerable.Range(1, 12)
            .Select(mes => new ResumoMensalFinanceiro
            {
                Mes = mes,
                Receita = receitaPorMes.TryGetValue(mes, out var receita) ? receita : 0m,
                Despesa = despesaPorMes.TryGetValue(mes, out var despesa) ? despesa : 0m
            })
            .ToList();

        return resultado;
    }

    private async Task<Dictionary<int, decimal>> ObterTotaisMensaisPorTipoAsync(
        DateTime dataInicio,
        DateTime dataFim,
        TipoLancamento tipo,
        int? produtorId,
        CancellationToken cancellationToken)
    {
        var queryLancamentos = CriarQueryFinanceiraBetween(dataInicio, dataFim, produtorId, null)
            .Where(item => item.Tipo == tipo);

        var existemLancamentos = await queryLancamentos.AnyAsync(cancellationToken);
        if (existemLancamentos)
        {
            var agrupadoLancamentos = await queryLancamentos
                .GroupBy(item => item.Data.Month)
                .Select(group => new
                {
                    Mes = group.Key,
                    Total = (decimal)group.Sum(item => (double)item.Valor)
                })
                .ToListAsync(cancellationToken);

            return agrupadoLancamentos.ToDictionary(item => item.Mes, item => item.Total);
        }

        var queryNotas = CriarQueryNotaFiscalEntreDatas(dataInicio, dataFim, produtorId, null)
            .Where(item => item.Tipo == tipo);

        var agrupadoNotas = await queryNotas
            .GroupBy(item => item.Data.Month)
            .Select(group => new
            {
                Mes = group.Key,
                Total = (decimal)group.Sum(item => (double)item.Valor)
            })
            .ToListAsync(cancellationToken);

        return agrupadoNotas.ToDictionary(item => item.Mes, item => item.Total);
    }

    public async Task<decimal> ObterTotalPorTipoAsync(DateTime dataInicio, DateTime dataFim, TipoLancamento tipo, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        return await ObterTotalFinanceiroAsync(dataInicio, dataFim, tipo, produtorId, null, cancellationToken);
    }

    public IAsyncEnumerable<Lancamento> StreamPorTipoAsync(DateTime dataInicio, DateTime dataFim, TipoLancamento tipo, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        return StreamFinanceiroAsync(dataInicio, dataFim, tipo, produtorId, null, cancellationToken);
    }

    public async Task<decimal> ObterTotalFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId = null, string? clienteFornecedorFiltro = null, CancellationToken cancellationToken = default)
    {
        var queryLancamentos = CriarQueryFinanceiraBetween(dataInicial, dataFinal, produtorId, clienteFornecedorFiltro)
            .Where(item => item.Tipo == tipo);

        var existemLancamentos = await queryLancamentos.AnyAsync(cancellationToken);
        if (existemLancamentos)
        {
            var totalLancamentos = await queryLancamentos.SumAsync(item => (double)item.Valor, cancellationToken);
            return (decimal)totalLancamentos;
        }

        var queryNotas = CriarQueryNotaFiscalEntreDatas(dataInicial, dataFinal, produtorId, clienteFornecedorFiltro)
            .Where(item => item.Tipo == tipo);

        var totalNotas = await queryNotas.SumAsync(item => (double)item.Valor, cancellationToken);
        return (decimal)totalNotas;
    }

    public async IAsyncEnumerable<Lancamento> StreamFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId = null, string? clienteFornecedorFiltro = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryLancamentos = CriarQueryFinanceiraBetween(dataInicial, dataFinal, produtorId, clienteFornecedorFiltro)
            .Where(item => item.Tipo == tipo)
            .OrderBy(item => item.Data)
            .AsNoTracking();

        var existemLancamentos = await queryLancamentos.AnyAsync(cancellationToken);
        if (existemLancamentos)
        {
            await foreach (var item in queryLancamentos.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        var queryNotas = CriarQueryNotaFiscalEntreDatas(dataInicial, dataFinal, produtorId, clienteFornecedorFiltro)
            .Where(item => item.Tipo == tipo)
            .OrderBy(item => item.Data)
            .AsNoTracking();

        await foreach (var item in queryNotas.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    private IQueryable<Lancamento> CriarQueryFinanceiraBetween(DateTime dataInicial, DateTime dataFinal, int? produtorId, string? clienteFornecedorFiltro)
    {
        var query = _dbContext.Lancamentos
            .Where(item => item.Data >= dataInicial && item.Data <= dataFinal);

        if (produtorId.HasValue)
        {
            query = query.Where(item => item.ProdutorId == produtorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(clienteFornecedorFiltro))
        {
            var filtro = clienteFornecedorFiltro.Trim();
            query = query.Where(item => item.ClienteFornecedor.Contains(filtro));
        }

        return query;
    }

    private IQueryable<Lancamento> CriarQueryNotaFiscalEntreDatas(DateTime dataInicial, DateTime dataFinal, int? produtorId, string? clienteFornecedorFiltro)
    {
        var query = _dbContext.NotasFiscais
            .Where(item => item.DataEmissao >= dataInicial && item.DataEmissao <= dataFinal);

        if (produtorId.HasValue)
        {
            query = query.Where(item => item.ProdutorId == produtorId.Value);
        }

        var queryProjetada = query.Select(item => new Lancamento
        {
            Data = item.DataEmissao,
            Tipo = item.TipoNota == TipoNota.Saida ? TipoLancamento.Receita : TipoLancamento.Despesa,
            ClienteFornecedor = item.TipoNota == TipoNota.Saida ? item.Destino : item.Origem,
            Descricao = item.Descricao,
            Situacao = "Concluído",
            Valor = item.ValorNotaFiscal,
            Vencimento = item.DataEmissao,
            ProdutorId = item.ProdutorId
        });

        if (!string.IsNullOrWhiteSpace(clienteFornecedorFiltro))
        {
            var filtro = clienteFornecedorFiltro.Trim();
            queryProjetada = queryProjetada.Where(item => item.ClienteFornecedor.Contains(filtro));
        }

        return queryProjetada;
    }
}
