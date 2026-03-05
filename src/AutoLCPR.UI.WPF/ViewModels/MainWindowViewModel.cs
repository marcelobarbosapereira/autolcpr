using System.ComponentModel;
using System.Windows.Input;
using AutoLCPR.UI.WPF.Commands;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para gerenciar a navegação da aplicação
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    OnPropertyChanged(nameof(CurrentView));
                }
            }
        }

        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToProdutorCommand { get; }
        public ICommand NavigateToNotaFiscalCommand { get; }
        public ICommand NavigateToRebanhoCommand { get; }
        public ICommand NavigateToRelatoriosCommand { get; }
        public ICommand NavigateToImportarCommand { get; }
        public ICommand NavigateToConfiguracoesCommand { get; }

        public MainWindowViewModel()
        {
            NavigateToHomeCommand = new RelayCommand(() => NavigateTo(new HomeViewModel()));
            NavigateToProdutorCommand = new RelayCommand(() => NavigateTo(new ProdutorViewModel()));
            NavigateToNotaFiscalCommand = new RelayCommand(() => NavigateTo(new NotaFiscalViewModel()));
            NavigateToRebanhoCommand = new RelayCommand(() => NavigateTo(new RebanhoViewModel()));
            NavigateToRelatoriosCommand = new RelayCommand(() => NavigateTo(new RelatoriosViewModel()));
            NavigateToImportarCommand = new RelayCommand(() => NavigateTo(new ImportarViewModel()));
            NavigateToConfiguracoesCommand = new RelayCommand(() => NavigateTo(new ConfiguracoesViewModel()));

            // Começar com a View Home
            CurrentView = new HomeViewModel();
        }

        private void NavigateTo(object viewModel)
        {
            CurrentView = viewModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
