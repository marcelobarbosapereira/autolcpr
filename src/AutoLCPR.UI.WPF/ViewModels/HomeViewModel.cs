namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para a tela inicial (Home)
    /// </summary>
    public class HomeViewModel
    {
        public int TotalRebanhos { get; set; } = 0;
        public int NotasFiscaisMes { get; set; } = 0;
        public decimal SaldoFinanceiro { get; set; } = 0m;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public HomeViewModel()
        {
            // TODO: Carregar dados do banco de dados
            LoadData();
        }

        /// <summary>
        /// Carrega os dados iniciais do dashboard
        /// </summary>
        private void LoadData()
        {
            // Dados mockados para demonstração
            // Será substituído por chamadas ao banco de dados
            TotalRebanhos = 0;
            NotasFiscaisMes = 0;
            SaldoFinanceiro = 0m;
        }
    }
}
