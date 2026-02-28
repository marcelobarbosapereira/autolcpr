using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class ProdutorRepository : IProdutorRepository
{
    private readonly AppDbContext _dbContext;

    public ProdutorRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Produtor?> GetByIdAsync(int id)
    {
        return await _dbContext.Produtores
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<Produtor>> GetAllAsync()
    {
        return await _dbContext.Produtores
            .AsNoTracking()
            .OrderBy(item => item.Nome)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Produtor entity)
    {
        await _dbContext.Produtores.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(Produtor entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.Produtores.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Produtores.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.Produtores.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Produtor?> GetByNomeAsync(string nome)
    {
        var normalized = nome.Trim().ToLower();
        return await _dbContext.Produtores
            .FirstOrDefaultAsync(item => item.Nome.ToLower() == normalized);
    }

    public async Task<Produtor?> GetByInscricaoEstadualAsync(string inscricaoEstadual)
    {
        var normalized = inscricaoEstadual.Trim().ToLower();
        return await _dbContext.Produtores
            .FirstOrDefaultAsync(item => item.InscricaoEstadual.ToLower() == normalized);
    }

    public async Task<IEnumerable<Produtor>> GetComRebanhos()
    {
        return await _dbContext.Produtores
            .AsNoTracking()
            .Include(item => item.Rebanhos)
            .OrderBy(item => item.Nome)
            .ToListAsync();
    }

    public async Task<IEnumerable<Produtor>> GetComNotasFiscais()
    {
        return await _dbContext.Produtores
            .AsNoTracking()
            .Include(item => item.NotasFiscais)
            .OrderBy(item => item.Nome)
            .ToListAsync();
    }
}
