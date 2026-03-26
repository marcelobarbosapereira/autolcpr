using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.UI.WPF.ViewModels;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
            
            // Registrar handlers de PreviewKeyDown para sincronizar seleções antes do Delete
            this.Loaded += (s, e) =>
            {
                if (DespesasDataGrid != null)
                    DespesasDataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
                if (ReceitasDataGrid != null)
                    ReceitasDataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            };
        }

        /// <summary>
        /// Sincroniza seleções ANTES que o comando Delete seja executado
        /// </summary>
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && sender is DataGrid dataGrid)
            {
                // Sincronizar todas as seleções AGORA
                if (DataContext is HomeViewModel viewModel)
                {
                    var notasSelecionadas = dataGrid.SelectedItems.OfType<NotaFiscal>().ToList();
                    viewModel.DefinirNotasSelecionadas(notasSelecionadas);
                }
            }
        }

        private void DespesasDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SincronizarNotasSelecionadas(sender as DataGrid);
        }

        private void ReceitasDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SincronizarNotasSelecionadas(sender as DataGrid);
        }

        private void SincronizarNotasSelecionadas(DataGrid? dataGrid)
        {
            if (DataContext is not HomeViewModel viewModel || dataGrid == null)
            {
                return;
            }

            var notasSelecionadas = dataGrid.SelectedItems.OfType<NotaFiscal>().ToList();
            viewModel.DefinirNotasSelecionadas(notasSelecionadas);
        }
    }
}
