using AutoLCPR.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para ProdutorView.xaml
    /// </summary>
    public partial class ProdutorView : UserControl
    {
        public ProdutorView()
        {
            InitializeComponent();
            DataContext = new ProdutorViewModel();
        }
    }
}
