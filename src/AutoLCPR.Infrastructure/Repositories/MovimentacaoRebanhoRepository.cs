using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class MovimentacaoRebanhoRepository : IMovimentacaoRebanhoRepository
{
    private readonly AppDbContext _dbContext;

    public MovimentacaoRebanhoRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MovimentacaoRebanho?> GetByIdAsync(int id)
    {
        return await _dbContext.MovimentacoesRebanho.FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<MovimentacaoRebanho>> GetAllAsync()
    {
        return await _dbContext.MovimentacoesRebanho
            .AsNoTracking()
            .OrderByDescending(item => item.Data)
            .ToListAsync();
    }

    public async Task<int> AddAsync(MovimentacaoRebanho entity)
    {
        await _dbContext.MovimentacoesRebanho.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(MovimentacaoRebanho entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.MovimentacoesRebanho.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.MovimentacoesRebanho.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.MovimentacoesRebanho.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ResumoMovimentacaoRebanho>> ObterResumoPorTipoAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        return await CriarQueryPorPeriodo(dataInicio, dataFim, produtorId)
            .GroupBy(item => item.TipoMovimentacao)
            .Select(group => new ResumoMovimentacaoRebanho
            {
                TipoMovimentacao = group.Key,
                Quantidade = group.Sum(item => item.Quantidade)
            })
            .OrderBy(item => item.TipoMovimentacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResumoConsolidadoRebanho> ObterResumoConsolidadoAnualAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        var resumo = await CriarQueryPorPeriodo(dataInicio, dataFim, produtorId)
            .GroupBy(item => item.TipoMovimentacao)
            .Select(group => new
            {
                TipoMovimentacao = group.Key,
                Quantidade = group.Sum(item => item.Quantidade)
            })
            .ToListAsync(cancellationToken);

        var totais = resumo.ToDictionary(item => item.TipoMovimentacao, item => item.Quantidade, StringComparer.OrdinalIgnoreCase);

        return new ResumoConsolidadoRebanho
        {
            TotalNascimentos = totais.TryGetValue("Nascimentos", out var nascimentos) ? nascimentos : 0,
            TotalCompras = totais.TryGetValue("Compras", out var compras) ? compras : 0,
            TotalVendas = totais.TryGetValue("Vendas", out var vendas) ? vendas : 0,
            TotalObitos = totais.TryGetValue("Óbitos", out var obitos) ? obitos : (totais.TryGetValue("Obitos", out var obitosSemAcento) ? obitosSemAcento : 0)
        };
    }

    public IAsyncEnumerable<MovimentacaoRebanho> StreamPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? produtorId = null, CancellationToken cancellationToken = default)
    {
        return CriarQueryPorPeriodo(dataInicio, dataFim, produtorId)
            .OrderBy(item => item.Data)
            .ThenBy(item => item.TipoMovimentacao)
            .AsNoTracking()
            .AsAsyncEnumerable();
    }

    private IQueryable<MovimentacaoRebanho> CriarQueryPorPeriodo(DateTime dataInicio, DateTime dataFim, int? produtorId)
    {
        var query = _dbContext.MovimentacoesRebanho
            .Where(item => item.Data >= dataInicio && item.Data <= dataFim);

        if (produtorId.HasValue)
        {
            query = query.Where(item => item.ProdutorId == produtorId.Value);
        }

        return query;
    }
}
