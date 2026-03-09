using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoLCPR.Application.Services;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para a tela de relatórios
    /// </summary>
    public class RelatoriosViewModel : INotifyPropertyChanged
    {
        private string _titulo = "Relatórios";
        private int _anoBase = DateTime.Now.Year;
        private string _status = "Pronto para gerar relatório";
        private bool _carregando = false;
        private IServiceProvider? _serviceProvider;
        private ImportacaoContextoService? _importacaoContextoService;

        public string Titulo
        {
            get => _titulo;
            set
            {
                if (_titulo != value)
                {
                    _titulo = value;
                    OnPropertyChanged(nameof(Titulo));
                }
            }
        }

        public int AnoBase
        {
            get => _anoBase;
            set
            {
                if (_anoBase != value)
                {
                    _anoBase = value;
                    OnPropertyChanged(nameof(AnoBase));
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

        public bool Carregando
        {
            get => _carregando;
            set
            {
                if (_carregando != value)
                {
                    _carregando = value;
                    OnPropertyChanged(nameof(Carregando));
                }
            }
        }

        public ICommand GerarLivroCaixaCommand { get; }

        public RelatoriosViewModel()
        {
            try
            {
                var app = System.Windows.Application.Current as App;
                if (app != null)
                {
                    _serviceProvider = app.ServiceProvider;
                    _importacaoContextoService = _serviceProvider?.GetService<ImportacaoContextoService>();
                }
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao inicializar ViewModel: {ex.Message}", "Erro", AlertType.Error);
            }

            GerarLivroCaixaCommand = new AsyncRelayCommand(GerarLivroCaixaAsync);
        }

        /// <summary>
        /// Gera o relatório do Livro Caixa em PDF
        /// </summary>
        private async Task GerarLivroCaixaAsync()
        {
            try
            {
                // Validar se um produtor foi selecionado
                if (_importacaoContextoService?.ProdutorSelecionadoId == null ||
                    string.IsNullOrWhiteSpace(_importacaoContextoService?.ProdutorSelecionadoNome))
                {
                    AlertService.Show("Selecione um Produtor Rural na Dashboard antes de gerar o relatório.", "Aviso", AlertType.Warning);
                    return;
                }

                if (AnoBase <= 0)
                {
                    AlertService.Show("Digite um ano base válido.", "Validação", AlertType.Warning);
                    return;
                }

                Carregando = true;
                Status = "Gerando relatório...";

                if (_serviceProvider == null)
                {
                    AlertService.Show("Serviço não inicializado.", "Erro", AlertType.Error);
                    return;
                }

                // Gerar o HTML do relatório
                using var scope = _serviceProvider.CreateScope();
                var relatorioService = new RelatorioService(scope.ServiceProvider);
                var caminhoHtml = await relatorioService.GerarLivroCaixaAsync(AnoBase, _importacaoContextoService.ProdutorSelecionadoId.Value);

                // Converter HTML para PDF usando Playwright
                await ConverterhtmlParaPdfAsync(caminhoHtml);

                Status = "Relatório gerado com sucesso!";
                AlertService.Show("Relatório gerado com sucesso!", "Sucesso", AlertType.Success);
            }
            catch (Exception ex)
            {
                Status = $"Erro: {ex.Message}";
                AlertService.Show($"Erro ao gerar relatório: {ex.Message}", "Erro", AlertType.Error);
            }
            finally
            {
                Carregando = false;
            }
        }

        /// <summary>
        /// Instala o Playwright automaticamente se necessário
        /// </summary>
        private async Task InstalarPlaywrightAsync()
        {
            try
            {
                var playwrightPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".playwright"
                );

                // Verificar se o Chromium já está instalado
                if (Directory.Exists(playwrightPath))
                {
                    var chromiumPath = Path.Combine(playwrightPath, "chromium");
                    if (Directory.Exists(chromiumPath))
                        return; // Já está instalado
                }

                // Se não estiver, tentar instalar em thread separada
                await Task.Run(() => Microsoft.Playwright.Program.Main(new[] { "install", "chromium" }));
            }
            catch (Exception ex)
            {
                // Se a instalação falhar, continuar mesmo assim
                System.Diagnostics.Debug.WriteLine($"Aviso: Erro ao instalar Playwright: {ex.Message}");
            }
        }

        /// <summary>
        /// Converte um arquivo HTML para PDF usando o Playwright
        /// </summary>
        private async Task ConverterhtmlParaPdfAsync(string caminhoHtml)
        {
            IBrowser? browser = null;
            try
            {
                // Instalar Playwright se necessário
                Status = "Instalando navegador (primeira vez)...";
                await InstalarPlaywrightAsync();

                // Inicializar Playwright
                Status = "Gerando PDF...";
                var playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync();
                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 800, Height = 1130 }
                });
                var page = await context.NewPageAsync();

                // Navegar para o arquivo HTML
                await page.GotoAsync($"file:///{caminhoHtml.Replace("\\", "/")}");

                // Gerar PDF
                var caminhoSaida = caminhoHtml.Replace(".html", ".pdf");
                await page.PdfAsync(new PagePdfOptions
                {
                    Path = caminhoSaida,
                    Format = "A4"
                });

                // Limpar
                await context.CloseAsync();

                // Abrir o PDF gerado
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = caminhoSaida,
                    UseShellExecute = true
                });
            }
            finally
            {
                if (browser != null)
                {
                    await browser.CloseAsync();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
