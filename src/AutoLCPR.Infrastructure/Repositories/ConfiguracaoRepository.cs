using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.Infrastructure.Repositories;

public class ConfiguracaoRepository : IConfiguracaoRepository
{
    private readonly AppDbContext _dbContext;

    public ConfiguracaoRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Configuracao?> GetByIdAsync(int id)
    {
        return await _dbContext.Configuracoes
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<IEnumerable<Configuracao>> GetAllAsync()
    {
        return await _dbContext.Configuracoes
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> AddAsync(Configuracao entity)
    {
        await _dbContext.Configuracoes.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(Configuracao entity)
    {
        entity.UpdatedAt = DateTime.Now;
        _dbContext.Configuracoes.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Configuracoes.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null)
        {
            return;
        }

        _dbContext.Configuracoes.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Obtém a configuração única do sistema (deve haver apenas uma)
    /// </summary>
    public async Task<Configuracao?> GetConfiguracaoAsync()
    {
        var config = await _dbContext.Configuracoes.FirstOrDefaultAsync();
        
        // Se não existe, criar uma configuração padrão
        if (config == null)
        {
            config = new Configuracao
            {
                ImagemCabecalhoRelatorios = null,
                CfopsIgnorados = null,
                NaturezasIgnoradas = null
            };
            await AddAsync(config);
        }

        return config;
    }
}
