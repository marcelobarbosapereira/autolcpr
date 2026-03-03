using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Application.DFe;
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
        private bool _isBaixandoXml;
        private int _downloadTotal;
        private int _downloadProcessadas;
        private int _downloadSucesso;
        private int _downloadFalhas;
        private double _downloadPercentual;
        private DateTime _dataInicio;
        private DateTime _dataFim;

        public ObservableCollection<string> ChavesImportadas { get; } = new();
        public Func<Task<IReadOnlyList<string>>>? CapturarChavesNoNavegadorAsync { get; set; }

        public ICommand ExecutarImportacaoCommand { get; }
        public ICommand BaixarXmlCommand { get; }

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

        public bool IsBaixandoXml
        {
            get => _isBaixandoXml;
            set
            {
                if (_isBaixandoXml != value)
                {
                    _isBaixandoXml = value;
                    OnPropertyChanged(nameof(IsBaixandoXml));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int DownloadTotal
        {
            get => _downloadTotal;
            set
            {
                if (_downloadTotal != value)
                {
                    _downloadTotal = value;
                    OnPropertyChanged(nameof(DownloadTotal));
                }
            }
        }

        public int DownloadProcessadas
        {
            get => _downloadProcessadas;
            set
            {
                if (_downloadProcessadas != value)
                {
                    _downloadProcessadas = value;
                    OnPropertyChanged(nameof(DownloadProcessadas));
                }
            }
        }

        public int DownloadSucesso
        {
            get => _downloadSucesso;
            set
            {
                if (_downloadSucesso != value)
                {
                    _downloadSucesso = value;
                    OnPropertyChanged(nameof(DownloadSucesso));
                }
            }
        }

        public int DownloadFalhas
        {
            get => _downloadFalhas;
            set
            {
                if (_downloadFalhas != value)
                {
                    _downloadFalhas = value;
                    OnPropertyChanged(nameof(DownloadFalhas));
                }
            }
        }

        public double DownloadPercentual
        {
            get => _downloadPercentual;
            set
            {
                if (Math.Abs(_downloadPercentual - value) > 0.01)
                {
                    _downloadPercentual = value;
                    OnPropertyChanged(nameof(DownloadPercentual));
                }
            }
        }

        public ImportarViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            _contextoImportacao = _serviceProvider?.GetService<ImportacaoContextoService>();

            DataInicio = _contextoImportacao?.DataInicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DataFim = _contextoImportacao?.DataFim ?? DateTime.Now.Date;

            ExecutarImportacaoCommand = new RelayCommand(() => _ = ExecutarImportacao(), () => !IsImportando && !IsBaixandoXml);
            BaixarXmlCommand = new RelayCommand(() => _ = BaixarXmlAsync(), () => !IsImportando && !IsBaixandoXml);
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

        public async Task BaixarXmlAsync()
        {
            if (_serviceProvider == null)
            {
                Status = "Serviços não inicializados.";
                return;
            }

            if (_contextoImportacao?.ProdutorSelecionadoId == null || _contextoImportacao.ProdutorSelecionadoId <= 0)
            {
                Status = "Nenhum produtor selecionado. Selecione um produtor na Dashboard antes de baixar XML.";
                return;
            }

            IsBaixandoXml = true;
            DownloadTotal = 0;
            DownloadProcessadas = 0;
            DownloadSucesso = 0;
            DownloadFalhas = 0;
            DownloadPercentual = 0;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var downloadService = scope.ServiceProvider.GetRequiredService<IDFePlaywrightDownloadService>();

                var progress = new Progress<DownloadXmlProgress>(item =>
                {
                    DownloadTotal = item.TotalNotas;
                    DownloadProcessadas = item.Processadas;
                    DownloadSucesso = item.Sucesso;
                    DownloadFalhas = item.Falhas;
                    DownloadPercentual = item.TotalNotas <= 0
                        ? 0
                        : (double)item.Processadas / item.TotalNotas * 100d;
                    Status = item.Mensagem;
                });

                await downloadService.BaixarXmlPendentesAsync(_contextoImportacao.ProdutorSelecionadoId.Value, progress);

                if (DownloadTotal == 0)
                {
                    Status = "Nenhuma nota pendente para baixar XML.";
                }
                else
                {
                    Status = $"Download finalizado. {DownloadSucesso}/{DownloadTotal} com sucesso e {DownloadFalhas} falha(s).";
                }
            }
            catch (Exception ex)
            {
                Status = $"Erro durante download de XML: {ex.Message}";
                MessageBox.Show(Status, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBaixandoXml = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
