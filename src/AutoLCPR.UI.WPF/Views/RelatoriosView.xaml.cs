using AutoLCPR.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para RelatoriosView.xaml
    /// </summary>
    public partial class RelatoriosView : UserControl
    {
        public RelatoriosView()
        {
            InitializeComponent();
            DataContext = new RelatoriosViewModel();
        }
    }
}
