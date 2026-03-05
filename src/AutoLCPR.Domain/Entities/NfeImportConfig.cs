namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Configurações para importação de NFes
    /// </summary>
    public class NfeImportConfig
    {
        /// <summary>
        /// Pasta contendo os arquivos HTML das NFes
        /// </summary>
        public string PastaHtml { get; set; } = string.Empty;

        /// <summary>
        /// Caminho da imagem para o cabeçalho dos relatórios
        /// </summary>
        public string? ImagemCabecalho { get; set; }

        /// <summary>
        /// CFOPs a serem ignorados (não inserir nota no banco)
        /// </summary>
        public List<string> IgnorarCFOP { get; set; } = new();

        /// <summary>
        /// Naturezas de Operação a serem ignoradas (não inserir nota no banco)
        /// </summary>
        public List<string> IgnorarNatureza { get; set; } = new();

        /// <summary>
        /// CFOPs que indicam RECEITA
        /// </summary>
        public List<string> CFOPReceita { get; set; } = new();

        /// <summary>
        /// CFOPs que indicam DESPESA
        /// </summary>
        public List<string> CFOPDespesa { get; set; } = new();

        /// <summary>
        /// Naturezas de Operação que indicam RECEITA
        /// </summary>
        public List<string> NaturezaReceita { get; set; } = new();

        /// <summary>
        /// Naturezas de Operação que indicam DESPESA
        /// </summary>
        public List<string> NaturezaDespesa { get; set; } = new();
    }
}
