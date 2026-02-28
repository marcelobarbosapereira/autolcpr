using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AutoLCPR.UI.WPF.ViewModels
{
    public class RelatoriosViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private int _anoFiltro = DateTime.Now.Year;
        private DateTime _dataInicio = new(DateTime.Now.Year, 1, 1);
        private DateTime _dataFim = new(DateTime.Now.Year, 12, 31);
        private string _relatorioSelecionado = "Receitas Mensais";

        public ObservableCollection<ResumoMensalItem> ReceitasMensais { get; } = new();
        public ObservableCollection<ResumoMensalItem> DespesasMensais { get; } = new();
        public ObservableCollection<ConsolidadoAnualItem> ConsolidadoAnual { get; } = new();
        public ObservableCollection<RelatorioInscricaoEstadualItem> RelatorioPorInscricaoEstadual { get; } = new();
        public ObservableCollection<SaldoRebanhoItem> SaldoRebanho { get; } = new();

        public IReadOnlyList<string> TiposRelatorioExportacao { get; } =
        [
            "Receitas Mensais",
            "Despesas Mensais",
            "Consolidado Anual",
            "Por Inscrição Estadual",
            "Saldo de Rebanho"
        ];

        public int AnoFiltro
        {
            get => _anoFiltro;
            set
            {
                if (_anoFiltro != value)
                {
                    _anoFiltro = value;
                    DataInicio = new DateTime(_anoFiltro, 1, 1);
                    DataFim = new DateTime(_anoFiltro, 12, 31);
                    OnPropertyChanged(nameof(AnoFiltro));
                }
            }
        }

        public DateTime DataInicio
        {
            get => _dataInicio;
            set
            {
                if (_dataInicio != value)
                {
                    _dataInicio = value;
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
                    OnPropertyChanged(nameof(DataFim));
                }
            }
        }

        public string RelatorioSelecionado
        {
            get => _relatorioSelecionado;
            set
            {
                if (_relatorioSelecionado != value)
                {
                    _relatorioSelecionado = value;
                    OnPropertyChanged(nameof(RelatorioSelecionado));
                }
            }
        }

        public ICommand AtualizarRelatoriosCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand ExportarExcelCommand { get; }

        public RelatoriosViewModel()
        {
            _serviceProvider = (Application.Current as App)?.ServiceProvider;

            AtualizarRelatoriosCommand = new RelayCommand(async () => await CarregarRelatoriosAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf);
            ExportarExcelCommand = new RelayCommand(ExportarExcel);

            _ = CarregarRelatoriosAsync();
        }

        private async Task CarregarRelatoriosAsync()
        {
            if (_serviceProvider == null)
            {
                AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                return;
            }

            if (DataInicio.Date > DataFim.Date)
            {
                AlertService.Show("A data inicial não pode ser maior que a data final.", "Validação", AlertType.Warning);
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var produtorRepo = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                var notaRepo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                var rebanhoRepo = scope.ServiceProvider.GetRequiredService<IRebanhoRepository>();

                var produtores = (await produtorRepo.GetAllAsync()).ToList();
                var notas = (await notaRepo.GetAllAsync()).ToList();
                var rebanhos = (await rebanhoRepo.GetAllAsync()).ToList();

                var notasFiltradas = notas
                    .Where(item => item.DataEmissao.Date >= DataInicio.Date && item.DataEmissao.Date <= DataFim.Date)
                    .ToList();

                var rebanhosFiltrados = rebanhos
                    .Where(item => item.CreatedAt.Date >= DataInicio.Date && item.CreatedAt.Date <= DataFim.Date)
                    .ToList();

                MontarRelatorioReceitasMensais(notasFiltradas);
                MontarRelatorioDespesasMensais(notasFiltradas);
                MontarRelatorioConsolidadoAnual(notasFiltradas);
                MontarRelatorioPorInscricaoEstadual(produtores, notasFiltradas);
                MontarRelatorioSaldoRebanho(produtores, rebanhosFiltrados);
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao carregar relatórios: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private void MontarRelatorioReceitasMensais(IEnumerable<NotaFiscal> notas)
        {
            ReceitasMensais.Clear();

            var totaisPorMes = notas
                .Where(item => item.TipoNota == TipoNota.Saida && item.DataEmissao.Year == AnoFiltro)
                .GroupBy(item => item.DataEmissao.Month)
                .ToDictionary(item => item.Key, item => item.Sum(x => x.ValorNotaFiscal));

            for (var mes = 1; mes <= 12; mes++)
            {
                totaisPorMes.TryGetValue(mes, out var totalMes);

                ReceitasMensais.Add(new ResumoMensalItem
                {
                    Mes = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(mes),
                    Total = totalMes
                });
            }
        }

        private void MontarRelatorioDespesasMensais(IEnumerable<NotaFiscal> notas)
        {
            DespesasMensais.Clear();

            var totaisPorMes = notas
                .Where(item => item.TipoNota == TipoNota.Entrada && item.DataEmissao.Year == AnoFiltro)
                .GroupBy(item => item.DataEmissao.Month)
                .ToDictionary(item => item.Key, item => item.Sum(x => x.ValorNotaFiscal));

            for (var mes = 1; mes <= 12; mes++)
            {
                totaisPorMes.TryGetValue(mes, out var totalMes);

                DespesasMensais.Add(new ResumoMensalItem
                {
                    Mes = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(mes),
                    Total = totalMes
                });
            }
        }

        private void MontarRelatorioConsolidadoAnual(IEnumerable<NotaFiscal> notas)
        {
            ConsolidadoAnual.Clear();

            var consolidado = notas
                .GroupBy(item => item.DataEmissao.Year)
                .OrderBy(item => item.Key)
                .Select(item =>
                {
                    var receitas = item.Where(x => x.TipoNota == TipoNota.Saida).Sum(x => x.ValorNotaFiscal);
                    var despesas = item.Where(x => x.TipoNota == TipoNota.Entrada).Sum(x => x.ValorNotaFiscal);

                    return new ConsolidadoAnualItem
                    {
                        Ano = item.Key,
                        Receitas = receitas,
                        Despesas = despesas,
                        Saldo = receitas - despesas
                    };
                });

            foreach (var item in consolidado)
            {
                ConsolidadoAnual.Add(item);
            }
        }

        private void MontarRelatorioPorInscricaoEstadual(IEnumerable<Produtor> produtores, IEnumerable<NotaFiscal> notas)
        {
            RelatorioPorInscricaoEstadual.Clear();

            var notasPorProdutor = notas
                .GroupBy(item => item.ProdutorId)
                .ToDictionary(item => item.Key, item => item.ToList());

            var linhas = produtores
                .OrderBy(item => item.Nome)
                .Select(produtor =>
                {
                    notasPorProdutor.TryGetValue(produtor.Id, out var notasProdutor);
                    notasProdutor ??= [];

                    var receitas = notasProdutor
                        .Where(item => item.TipoNota == TipoNota.Saida)
                        .Sum(item => item.ValorNotaFiscal);

                    var despesas = notasProdutor
                        .Where(item => item.TipoNota == TipoNota.Entrada)
                        .Sum(item => item.ValorNotaFiscal);

                    return new RelatorioInscricaoEstadualItem
                    {
                        InscricaoEstadual = string.IsNullOrWhiteSpace(produtor.InscricaoEstadual)
                            ? "NAO INFORMADA"
                            : produtor.InscricaoEstadual,
                        Produtor = produtor.Nome,
                        Receitas = receitas,
                        Despesas = despesas,
                        Saldo = receitas - despesas
                    };
                });

            foreach (var item in linhas)
            {
                RelatorioPorInscricaoEstadual.Add(item);
            }
        }

        private void MontarRelatorioSaldoRebanho(IEnumerable<Produtor> produtores, IEnumerable<Rebanho> rebanhos)
        {
            SaldoRebanho.Clear();

            var produtorPorId = produtores.ToDictionary(item => item.Id, item => item);

            foreach (var item in rebanhos.OrderBy(x => x.NomeRebanho))
            {
                produtorPorId.TryGetValue(item.ProdutorId, out var produtor);

                var saldo = item.Entradas + item.Nascimentos - item.Saidas - item.Mortes;

                SaldoRebanho.Add(new SaldoRebanhoItem
                {
                    InscricaoEstadual = produtor?.InscricaoEstadual ?? "NAO INFORMADA",
                    Produtor = produtor?.Nome ?? "Produtor não encontrado",
                    IdRebanho = item.IdRebanho,
                    NomeRebanho = item.NomeRebanho,
                    Entradas = item.Entradas,
                    Nascimentos = item.Nascimentos,
                    Saidas = item.Saidas,
                    Mortes = item.Mortes,
                    Saldo = saldo
                });
            }
        }

        private void ExportarPdf()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Arquivo PDF (*.pdf)|*.pdf",
                    FileName = $"relatorio-{DateTime.Now:yyyyMMddHHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }

                var exportData = ObterDadosExportacao();
                ReportExportService.ExportarPdf(saveFileDialog.FileName, exportData.Titulo, exportData.Cabecalhos, exportData.Linhas);

                AlertService.Show("Relatório PDF exportado com sucesso!", "Sucesso", AlertType.Success);
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao exportar PDF: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private void ExportarExcel()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Arquivo Excel (*.xlsx)|*.xlsx",
                    FileName = $"relatorio-{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }

                var exportData = ObterDadosExportacao();
                ReportExportService.ExportarExcel(saveFileDialog.FileName, exportData.Titulo, exportData.Cabecalhos, exportData.Linhas);

                AlertService.Show("Relatório Excel exportado com sucesso!", "Sucesso", AlertType.Success);
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao exportar Excel: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private (string Titulo, string[] Cabecalhos, List<string[]> Linhas) ObterDadosExportacao()
        {
            return RelatorioSelecionado switch
            {
                "Receitas Mensais" =>
                    (
                        $"Relatório Mensal de Receitas - {AnoFiltro} ({DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy})",
                        ["Mês", "Total"],
                        ReceitasMensais.Select(item => new[] { item.Mes, item.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) }).ToList()
                    ),
                "Despesas Mensais" =>
                    (
                        $"Relatório Mensal de Despesas - {AnoFiltro} ({DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy})",
                        ["Mês", "Total"],
                        DespesasMensais.Select(item => new[] { item.Mes, item.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) }).ToList()
                    ),
                "Consolidado Anual" =>
                    (
                        $"Relatório Anual Consolidado ({DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy})",
                        ["Ano", "Receitas", "Despesas", "Saldo"],
                        ConsolidadoAnual.Select(item => new[]
                        {
                            item.Ano.ToString(),
                            item.Receitas.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
                            item.Despesas.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
                            item.Saldo.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))
                        }).ToList()
                    ),
                "Por Inscrição Estadual" =>
                    (
                        $"Relatório por Inscrição Estadual ({DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy})",
                        ["Inscrição Estadual", "Produtor", "Receitas", "Despesas", "Saldo"],
                        RelatorioPorInscricaoEstadual.Select(item => new[]
                        {
                            item.InscricaoEstadual,
                            item.Produtor,
                            item.Receitas.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
                            item.Despesas.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
                            item.Saldo.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))
                        }).ToList()
                    ),
                _ =>
                    (
                        $"Relatório de Saldo de Rebanho ({DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy})",
                        ["Inscrição Estadual", "Produtor", "ID Rebanho", "Nome Rebanho", "Entradas", "Nascimentos", "Saídas", "Mortes", "Saldo"],
                        SaldoRebanho.Select(item => new[]
                        {
                            item.InscricaoEstadual,
                            item.Produtor,
                            item.IdRebanho,
                            item.NomeRebanho,
                            item.Entradas.ToString(),
                            item.Nascimentos.ToString(),
                            item.Saidas.ToString(),
                            item.Mortes.ToString(),
                            item.Saldo.ToString()
                        }).ToList()
                    )
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ResumoMensalItem
    {
        public string Mes { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class ConsolidadoAnualItem
    {
        public int Ano { get; set; }
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public decimal Saldo { get; set; }
    }

    public class RelatorioInscricaoEstadualItem
    {
        public string InscricaoEstadual { get; set; } = string.Empty;
        public string Produtor { get; set; } = string.Empty;
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public decimal Saldo { get; set; }
    }

    public class SaldoRebanhoItem
    {
        public string InscricaoEstadual { get; set; } = string.Empty;
        public string Produtor { get; set; } = string.Empty;
        public string IdRebanho { get; set; } = string.Empty;
        public string NomeRebanho { get; set; } = string.Empty;
        public int Entradas { get; set; }
        public int Nascimentos { get; set; }
        public int Saidas { get; set; }
        public int Mortes { get; set; }
        public int Saldo { get; set; }
    }
}
