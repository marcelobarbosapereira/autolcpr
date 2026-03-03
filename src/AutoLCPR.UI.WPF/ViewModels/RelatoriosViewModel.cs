using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using AutoLCPR.Application.Relatorios;
using AutoLCPR.Domain.Entities;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para a tela de relatórios
    /// </summary>
    public class RelatoriosViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private int _anoFiscal;
        private DateTime _dataInicial;
        private DateTime _dataFinal;
        private TipoLancamento _tipoLancamentoFinanceiro;
        private string _status = "Selecione o ano-base e clique em Gerar Relatório.";

        public int AnoFiscal
        {
            get => _anoFiscal;
            set
            {
                if (_anoFiscal != value)
                {
                    _anoFiscal = value;
                    OnPropertyChanged(nameof(AnoFiscal));
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

        public DateTime DataInicial
        {
            get => _dataInicial;
            set
            {
                if (_dataInicial != value)
                {
                    _dataInicial = value;
                    OnPropertyChanged(nameof(DataInicial));
                }
            }
        }

        public DateTime DataFinal
        {
            get => _dataFinal;
            set
            {
                if (_dataFinal != value)
                {
                    _dataFinal = value;
                    OnPropertyChanged(nameof(DataFinal));
                }
            }
        }

        public TipoLancamento TipoLancamentoFinanceiro
        {
            get => _tipoLancamentoFinanceiro;
            set
            {
                if (_tipoLancamentoFinanceiro != value)
                {
                    _tipoLancamentoFinanceiro = value;
                    OnPropertyChanged(nameof(TipoLancamentoFinanceiro));
                }
            }
        }

        public IReadOnlyList<TipoLancamento> TiposLancamentoFinanceiro { get; }

        public ICommand GerarRelatorioCommand { get; }
        public ICommand GerarRelatorioRebanhoCommand { get; }
        public ICommand GerarRelatorioFinanceiroCommand { get; }

        public RelatoriosViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            _anoFiscal = DateTime.Now.Year - 1;
            _dataInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _dataFinal = DateTime.Now.Date;
            _tipoLancamentoFinanceiro = TipoLancamento.Receita;
            TiposLancamentoFinanceiro = new List<TipoLancamento> { TipoLancamento.Receita, TipoLancamento.Despesa };
            GerarRelatorioCommand = new RelayCommand(GerarRelatorioAnualPdf);
            GerarRelatorioRebanhoCommand = new RelayCommand(GerarRelatorioRebanhoPdf);
            GerarRelatorioFinanceiroCommand = new RelayCommand(GerarRelatorioFinanceiroPdf);
        }

        private void GerarRelatorioAnualPdf()
        {
            GerarPdf(
                "Gerando relatório anual em PDF...",
                "Salvar Relatório Consolidado Anual",
                $"LivroCaixa_{AnoFiscal}_{AnoFiscal + 1}.pdf",
                serviceProvider => serviceProvider.GetRequiredService<IRelatorioAnualService>().GerarRelatorioAnual(AnoFiscal),
                "Erro ao gerar o relatório anual.");
        }

        private void GerarRelatorioRebanhoPdf()
        {
            GerarPdf(
                "Gerando relatório de movimentação de rebanho em PDF...",
                "Salvar Relatório de Movimentação de Rebanho",
                $"Rebanho_{AnoFiscal}_{AnoFiscal + 1}.pdf",
                serviceProvider => serviceProvider.GetRequiredService<IRelatorioRebanhoService>().GerarRelatorioRebanho(AnoFiscal),
                "Erro ao gerar o relatório de rebanho.");
        }

        private void GerarRelatorioFinanceiroPdf()
        {
            if (DataInicial.Date > DataFinal.Date)
            {
                AlertService.Show("A data inicial não pode ser maior que a data final.", "Validação", AlertType.Warning);
                return;
            }

            GerarPdf(
                "Gerando relatório financeiro por período em PDF...",
                "Salvar Relatório Financeiro por Período",
                $"{TipoLancamentoFinanceiro}_{DataInicial:yyyyMMdd}_{DataFinal:yyyyMMdd}.pdf",
                serviceProvider => serviceProvider
                    .GetRequiredService<IRelatorioFinanceiroService>()
                    .GerarRelatorioFinanceiro(DataInicial.Date, DataFinal.Date, TipoLancamentoFinanceiro),
                "Erro ao gerar o relatório financeiro por período.");
        }

        private void GerarPdf(string statusProcessando, string tituloSalvar, string nomeArquivo, Func<IServiceProvider, byte[]> gerarPdf, string statusErro)
        {
            if (_serviceProvider == null)
            {
                AlertService.Show("Serviços não inicializados.", "Erro", AlertType.Error);
                return;
            }

            if (AnoFiscal < 1900 || AnoFiscal > 3000)
            {
                AlertService.Show("Informe um ano-base válido.", "Validação", AlertType.Warning);
                return;
            }

            try
            {
                Status = statusProcessando;

                using var scope = _serviceProvider.CreateScope();
                var pdfBytes = gerarPdf(scope.ServiceProvider);

                if (pdfBytes.Length == 0)
                {
                    AlertService.Show("Não foi possível gerar o PDF.", "Erro", AlertType.Error);
                    Status = "Falha na geração do relatório.";
                    return;
                }

                var fileDialog = new SaveFileDialog
                {
                    Title = tituloSalvar,
                    Filter = "Arquivo PDF (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    FileName = nomeArquivo
                };

                if (fileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(fileDialog.FileName, pdfBytes);
                    AlertService.Show("Relatório gerado com sucesso!", "Sucesso", AlertType.Success);
                    Status = $"Relatório salvo em: {fileDialog.FileName}";
                }
                else
                {
                    Status = "Geração concluída, mas o salvamento foi cancelado.";
                }
            }
            catch (Exception ex)
            {
                Status = statusErro;
                var detalhe = ex.ToString();
                if (detalhe.Length > 1400)
                {
                    detalhe = detalhe[..1400] + "...";
                }

                AlertService.Show($"Erro ao gerar relatório: {detalhe}", "Erro", AlertType.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
