using System.Windows.Controls;
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
    }
}
