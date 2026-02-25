using AutoLCPR.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para RebanhoView.xaml
    /// </summary>
    public partial class RebanhoView : UserControl
    {
        public RebanhoView()
        {
            InitializeComponent();
            DataContext = new RebanhoViewModel();
        }
    }
}
