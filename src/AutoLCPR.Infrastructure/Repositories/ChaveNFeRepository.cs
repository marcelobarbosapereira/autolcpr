using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class ChaveNFeRepository : IChaveNFeRepository
{
    private readonly AppDbContext _dbContext;

    public ChaveNFeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChaveNFe?> GetByIdAsync(int id)
    {
        return await _dbContext.ChavesNFe
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<ChaveNFe>> GetAllAsync()
    {
        return await _dbContext.ChavesNFe
            .AsNoTracking()
            .OrderByDescending(item => item.DataImportacao)
            .ToListAsync();
    }

    public async Task<int> AddAsync(ChaveNFe entity)
    {
        await _dbContext.ChavesNFe.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task AddRangeAsync(IEnumerable<ChaveNFe> entities)
    {
        var itens = entities.ToList();
        if (itens.Count == 0)
        {
            return;
        }

        var produtorId = itens[0].ProdutorId;
        var chaves = itens.Select(item => item.ChaveAcesso).Distinct().ToList();

        var existentes = await _dbContext.ChavesNFe
            .Where(item => item.ProdutorId == produtorId && chaves.Contains(item.ChaveAcesso))
            .Select(item => item.ChaveAcesso)
            .ToListAsync();

        var novos = itens
            .Where(item => !existentes.Contains(item.ChaveAcesso))
            .ToList();

        if (novos.Count == 0)
        {
            return;
        }

        await _dbContext.ChavesNFe.AddRangeAsync(novos);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChaveNFe entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.ChavesNFe.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.ChavesNFe.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.ChavesNFe.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ChaveNFe>> GetByProdutorIdAsync(int produtorId)
    {
        return await _dbContext.ChavesNFe
            .AsNoTracking()
            .Where(item => item.ProdutorId == produtorId)
            .OrderByDescending(item => item.DataImportacao)
            .ToListAsync();
    }
}
