using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Application.Services;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para a tela de configurações
    /// </summary>
    public class ConfiguracoesViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly NfeConfigService? _configService;
        private string? _pastaHtml;
        private string? _imagemCabecalho;
        private string _cfopsIgnorados = string.Empty;
        private string _naturezasIgnoradas = string.Empty;
        private string _cfopReceita = string.Empty;
        private string _cfopDespesa = string.Empty;
        private string _naturezaReceita = string.Empty;
        private string _naturezaDespesa = string.Empty;
        private string _status = "Carregando configurações...";

        public string? PastaHtml
        {
            get => _pastaHtml;
            set
            {
                if (_pastaHtml != value)
                {
                    _pastaHtml = value;
                    OnPropertyChanged(nameof(PastaHtml));
                }
            }
        }

        public string? ImagemCabecalho
        {
            get => _imagemCabecalho;
            set
            {
                if (_imagemCabecalho != value)
                {
                    _imagemCabecalho = value;
                    OnPropertyChanged(nameof(ImagemCabecalho));
                }
            }
        }

        public string CfopsIgnorados
        {
            get => _cfopsIgnorados;
            set
            {
                if (_cfopsIgnorados != value)
                {
                    _cfopsIgnorados = value;
                    OnPropertyChanged(nameof(CfopsIgnorados));
                }
            }
        }

        public string NaturezasIgnoradas
        {
            get => _naturezasIgnoradas;
            set
            {
                if (_naturezasIgnoradas != value)
                {
                    _naturezasIgnoradas = value;
                    OnPropertyChanged(nameof(NaturezasIgnoradas));
                }
            }
        }

        public string CfopReceita
        {
            get => _cfopReceita;
            set
            {
                if (_cfopReceita != value)
                {
                    _cfopReceita = value;
                    OnPropertyChanged(nameof(CfopReceita));
                }
            }
        }

        public string CfopDespesa
        {
            get => _cfopDespesa;
            set
            {
                if (_cfopDespesa != value)
                {
                    _cfopDespesa = value;
                    OnPropertyChanged(nameof(CfopDespesa));
                }
            }
        }

        public string NaturezaReceita
        {
            get => _naturezaReceita;
            set
            {
                if (_naturezaReceita != value)
                {
                    _naturezaReceita = value;
                    OnPropertyChanged(nameof(NaturezaReceita));
                }
            }
        }

        public string NaturezaDespesa
        {
            get => _naturezaDespesa;
            set
            {
                if (_naturezaDespesa != value)
                {
                    _naturezaDespesa = value;
                    OnPropertyChanged(nameof(NaturezaDespesa));
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

        public ICommand SalvarCommand { get; }
        public ICommand SelecionarImagemCommand { get; }
        public ICommand LimparImagemCommand { get; }

        public ConfiguracoesViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            _configService = _serviceProvider?.GetService<NfeConfigService>();

            SalvarCommand = new RelayCommand(async () => await SalvarAsync());
            SelecionarImagemCommand = new RelayCommand(SelecionarImagem);
            LimparImagemCommand = new RelayCommand(() => ImagemCabecalho = null);

            // Carregar configurações existentes
            _ = CarregarConfiguracoes();
        }

        private async Task CarregarConfiguracoes()
        {
            if (_configService == null)
            {
                Status = "Erro: Serviço de configuração não inicializado.";
                return;
            }

            try
            {
                Status = "Carregando configurações...";
                var config = await _configService.CarregarConfiguracaoAsync();

                PastaHtml = config.PastaHtml;
                ImagemCabecalho = config.ImagemCabecalho;
                CfopsIgnorados = string.Join(", ", config.IgnorarCFOP);
                NaturezasIgnoradas = string.Join(", ", config.IgnorarNatureza);
                CfopReceita = string.Join(", ", config.CFOPReceita);
                CfopDespesa = string.Join(", ", config.CFOPDespesa);
                NaturezaReceita = string.Join(", ", config.NaturezaReceita);
                NaturezaDespesa = string.Join(", ", config.NaturezaDespesa);

                Status = "Configurações carregadas com sucesso.";
            }
            catch (Exception ex)
            {
                Status = $"Erro ao carregar configurações: {ex.Message}";
                AlertService.Show($"Erro ao carregar: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private async Task SalvarAsync()
        {
            if (_configService == null)
            {
                AlertService.Show("Serviço de configuração não inicializado.", "Erro", AlertType.Error);
                return;
            }

            try
            {
                Status = "Salvando configurações...";

                var config = new NfeImportConfig
                {
                    PastaHtml = PastaHtml ?? "%AppData%/AutoLCPR/html",
                    ImagemCabecalho = ImagemCabecalho,
                    IgnorarCFOP = ParsearListaString(CfopsIgnorados),
                    IgnorarNatureza = ParsearListaString(NaturezasIgnoradas),
                    CFOPReceita = ParsearListaString(CfopReceita),
                    CFOPDespesa = ParsearListaString(CfopDespesa),
                    NaturezaReceita = ParsearListaString(NaturezaReceita),
                    NaturezaDespesa = ParsearListaString(NaturezaDespesa)
                };

                await _configService.SalvarConfiguracaoAsync(config);
                _configService.LimparCache();

                Status = "Configurações salvas com sucesso!";
                AlertService.Show("Configurações salvas com sucesso!", "Sucesso", AlertType.Success);
            }
            catch (Exception ex)
            {
                Status = $"Erro ao salvar: {ex.Message}";
                AlertService.Show($"Erro ao salvar: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private void SelecionarImagem()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecionar Imagem para Cabeçalho",
                Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos os arquivos|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                ImagemCabecalho = dialog.FileName;
            }
        }

        private List<string> ParsearListaString(string texto)
        {
            return texto.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
