using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade que representa as configurações do sistema
    /// </summary>
    public class Configuracao : BaseEntity
    {
        /// <summary>
        /// Caminho da imagem para o cabeçalho dos relatórios
        /// </summary>
        public string? ImagemCabecalhoRelatorios { get; set; }

        /// <summary>
        /// CFOPs a serem ignorados durante a importação (separados por vírgula)
        /// </summary>
        public string? CfopsIgnorados { get; set; }

        /// <summary>
        /// Naturezas de Operação a serem ignoradas durante a importação (separadas por vírgula)
        /// </summary>
        public string? NaturezasIgnoradas { get; set; }
    }
}
