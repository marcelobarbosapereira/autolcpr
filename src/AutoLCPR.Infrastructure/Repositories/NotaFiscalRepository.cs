using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly AppDbContext _dbContext;

    public NotaFiscalRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotaFiscal?> GetByIdAsync(int id)
    {
        return await _dbContext.NotasFiscais
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<NotaFiscal>> GetAllAsync()
    {
        return await _dbContext.NotasFiscais
            .AsNoTracking()
            .OrderByDescending(item => item.DataEmissao)
            .ToListAsync();
    }

    public async Task<int> AddAsync(NotaFiscal entity)
    {
        await _dbContext.NotasFiscais.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(NotaFiscal entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.NotasFiscais.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.NotasFiscais.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.NotasFiscais.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<NotaFiscal>> GetByProdutorIdAsync(int produtorId)
    {
        return await _dbContext.NotasFiscais
            .AsNoTracking()
            .Where(item => item.ProdutorId == produtorId)
            .OrderByDescending(item => item.DataEmissao)
            .ToListAsync();
    }

    public async Task<NotaFiscal?> GetByChaveAcessoAsync(string chaveAcesso)
    {
        return await _dbContext.NotasFiscais
            .FirstOrDefaultAsync(item => item.ChaveAcesso == chaveAcesso);
    }

    public async Task<IEnumerable<NotaFiscal>> GetByTipoAsync(TipoNota tipo)
    {
        return await _dbContext.NotasFiscais
            .AsNoTracking()
            .Where(item => item.TipoNota == tipo)
            .OrderByDescending(item => item.DataEmissao)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotaFiscal>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim)
    {
        return await _dbContext.NotasFiscais
            .AsNoTracking()
            .Where(item => item.DataEmissao.Date >= dataInicio.Date && item.DataEmissao.Date <= dataFim.Date)
            .OrderByDescending(item => item.DataEmissao)
            .ToListAsync();
    }
}
