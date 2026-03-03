using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class RebanhoRepository : IRebanhoRepository
{
    private readonly AppDbContext _dbContext;

    public RebanhoRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Rebanho?> GetByIdAsync(int id)
    {
        return await _dbContext.Rebanhos
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<Rebanho>> GetAllAsync()
    {
        return await _dbContext.Rebanhos
            .AsNoTracking()
            .OrderBy(item => item.NomeRebanho)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Rebanho entity)
    {
        await _dbContext.Rebanhos.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(Rebanho entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.Rebanhos.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Rebanhos.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.Rebanhos.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Rebanho>> GetByProdutorIdAsync(int produtorId)
    {
        return await _dbContext.Rebanhos
            .AsNoTracking()
            .Where(item => item.ProdutorId == produtorId)
            .OrderBy(item => item.NomeRebanho)
            .ToListAsync();
    }

    public async Task<Rebanho?> GetByIdRebanhoAsync(string idRebanho)
    {
        return await _dbContext.Rebanhos
            .FirstOrDefaultAsync(item => item.IdRebanho == idRebanho);
    }
}
