using AutoLCPR.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para ConfiguracoesView.xaml
    /// </summary>
    public partial class ConfiguracoesView : UserControl
    {
        public ConfiguracoesView()
        {
            InitializeComponent();
            DataContext = new ConfiguracoesViewModel();
        }
    }
}
