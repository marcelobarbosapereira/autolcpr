using AutoLCPR.UI.WPF.ViewModels;
using System.Windows.Controls;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para ImportarView.xaml
    /// </summary>
    public partial class ImportarView : UserControl
    {
        public ImportarView()
        {
            InitializeComponent();
            DataContext = new ImportarViewModel();
        }
    }
}
