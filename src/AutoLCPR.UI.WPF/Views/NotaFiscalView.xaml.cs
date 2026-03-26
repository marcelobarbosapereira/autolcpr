using AutoLCPR.UI.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para NotaFiscalView.xaml
    /// </summary>
    public partial class NotaFiscalView : UserControl
    {
        public NotaFiscalView()
        {
            InitializeComponent();
            DataContext = new NotaFiscalViewModel();
        }

        private void FecharJanela_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
