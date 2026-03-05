using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Domain.Repositories
{
    /// <summary>
    /// Interface para repositório de configurações
    /// </summary>
    public interface IConfiguracaoRepository : IRepository<Configuracao>
    {
        /// <summary>
        /// Obtém a configuração única do sistema (deve haver apenas uma)
        /// </summary>
        Task<Configuracao?> GetConfiguracaoAsync();
    }
}
