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
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && DataContext is HomeViewModel viewModel)
            {
                // Tratar seleção de Rebanho
                if (element.DataContext is Rebanho rebanho)
                {
                    viewModel.SelecionarRebanhoItemCommand.Execute(rebanho);
                }
                // Tratar seleção de NotaFiscal
                else if (element.DataContext is NotaFiscal nota)
                {
                    viewModel.SelecionarNotaFiscalCommand.Execute(nota);
                }
            }
        }
    }
}
