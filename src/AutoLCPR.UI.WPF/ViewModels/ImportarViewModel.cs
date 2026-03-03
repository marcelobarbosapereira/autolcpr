using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AutoLCPR.UI.WPF.ViewModels
{
    public class ImportarViewModel : INotifyPropertyChanged
    {
        public const string SefazUrl = "http://eservicos.sefaz.ms.gov.br/";

        private readonly IServiceProvider? _serviceProvider;
        private readonly ImportacaoContextoService? _contextoImportacao;
        private string _status = "Abra o site no painel para iniciar.";
        private bool _isImportando;
        private DateTime _dataInicio;
        private DateTime _dataFim;

        public ObservableCollection<string> ChavesImportadas { get; } = new();
        public Func<Task<IReadOnlyList<string>>>? CapturarChavesNoNavegadorAsync { get; set; }

        public ICommand ExecutarImportacaoCommand { get; }

        public DateTime DataInicio
        {
            get => _dataInicio;
            set
            {
                if (_dataInicio != value)
                {
                    _dataInicio = value;
                    if (_contextoImportacao != null)
                    {
                        _contextoImportacao.DataInicio = value;
                    }
                    OnPropertyChanged(nameof(DataInicio));
                }
            }
        }

        public DateTime DataFim
        {
            get => _dataFim;
            set
            {
                if (_dataFim != value)
                {
                    _dataFim = value;
                    if (_contextoImportacao != null)
                    {
                        _contextoImportacao.DataFim = value;
                    }
                    OnPropertyChanged(nameof(DataFim));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public bool IsImportando
        {
            get => _isImportando;
            set
            {
                if (_isImportando != value)
                {
                    _isImportando = value;
                    OnPropertyChanged(nameof(IsImportando));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ImportarViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            _contextoImportacao = _serviceProvider?.GetService<ImportacaoContextoService>();

            DataInicio = _contextoImportacao?.DataInicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DataFim = _contextoImportacao?.DataFim ?? DateTime.Now.Date;

            ExecutarImportacaoCommand = new RelayCommand(() => _ = ExecutarImportacao(), () => !IsImportando);
        }

        public async Task ExecutarImportacao()
        {
            if (_serviceProvider == null)
            {
                Status = "Serviços não inicializados.";
                return;
            }

            if (_contextoImportacao?.ProdutorSelecionadoId == null || _contextoImportacao.ProdutorSelecionadoId <= 0)
            {
                Status = "Nenhum produtor selecionado. Selecione um produtor na Dashboard antes de importar.";
                return;
            }

            if (CapturarChavesNoNavegadorAsync == null)
            {
                Status = "Navegador interno não inicializado.";
                return;
            }

            if (DataInicio.Date > DataFim.Date)
            {
                Status = "Período inválido: Data Início maior que Data Fim.";
                return;
            }

            IsImportando = true;
            Status = "Capturando chaves da página atual...";

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var chaveRepository = scope.ServiceProvider.GetRequiredService<IChaveNFeRepository>();

                var chaves = await CapturarChavesNoNavegadorAsync();
                if (chaves.Count == 0)
                {
                    Status = "Nenhuma chave encontrada na página. Verifique se a tabela foi carregada.";
                    return;
                }

                ChavesImportadas.Clear();
                foreach (var chave in chaves)
                {
                    ChavesImportadas.Add(chave);
                }

                var produtorId = _contextoImportacao.ProdutorSelecionadoId.Value;
                var dataImportacao = DateTime.Now;

                var entidades = chaves.Select(chave => new ChaveNFe
                {
                    ProdutorId = produtorId,
                    ChaveAcesso = chave,
                    DataImportacao = dataImportacao
                });

                await chaveRepository.AddRangeAsync(entidades);

                Status = $"Importação finalizada com sucesso. {chaves.Count} chave(s) processada(s) para o produtor {produtorId}.";
            }
            catch (Exception ex)
            {
                Status = $"Erro durante importação: {ex.Message}";
                MessageBox.Show(Status, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsImportando = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
