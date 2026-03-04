using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Text.RegularExpressions;
using System.IO;

namespace AutoLCPR.UI.WPF.ViewModels
{
    public class ImportarViewModel : INotifyPropertyChanged
    {
        public const string SefazUrl = "http://eservicos.sefaz.ms.gov.br/";
        private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);

        private readonly IServiceProvider? _serviceProvider;
        private readonly ImportacaoContextoService? _contextoImportacao;
        private string _status = "Abra o site no painel para iniciar.";
        private bool _isImportando;
        private DateTime _dataInicio;
        private DateTime _dataFim;

        public ObservableCollection<string> ChavesImportadas { get; } = new();
        public Func<Task<IReadOnlyList<SefazNFeCapturada>>>? CapturarNotasNoNavegadorAsync { get; set; }

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

            if (CapturarNotasNoNavegadorAsync == null)
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
            Status = "Capturando NF-es de todas as páginas da consulta...";

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notaRepository = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                var produtorRepository = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();

                var notasCapturadas = await CapturarNotasNoNavegadorAsync();
                if (notasCapturadas.Count == 0)
                {
                    Status = "Nenhuma NF-e encontrada na página. Verifique se a consulta foi carregada.";
                    return;
                }

                var chaves = notasCapturadas
                    .Select(item => item.ChaveAcesso)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct()
                    .ToList();

                if (chaves.Count == 0)
                {
                    Status = "Nenhuma chave de acesso válida encontrada na resposta da SEFAZ.";
                    return;
                }

                ChavesImportadas.Clear();
                foreach (var chave in chaves)
                {
                    ChavesImportadas.Add(chave);
                }

                var produtorId = _contextoImportacao.ProdutorSelecionadoId.Value;
                var dataImportacao = DateTime.Now;
                var produtor = await produtorRepository.GetByIdAsync(produtorId);

                if (produtor == null)
                {
                    Status = "Produtor selecionado não encontrado.";
                    return;
                }

                var cpfProdutor = NormalizarSomenteDigitos(produtor.Cpf);
                if (cpfProdutor.Length != 11)
                {
                    Status = "CPF do produtor selecionado não está válido. Atualize o cadastro do produtor antes de importar.";
                    return;
                }

                var diretorioHtmlConsulta = CriarDiretorioConsultaHtml(cpfProdutor);

                var novasNotas = 0;
                var notasAtualizadas = 0;
                var notasIgnoradas = 0;
                var htmlSalvos = 0;

                foreach (var item in notasCapturadas.GroupBy(item => item.ChaveAcesso).Select(item => item.First()))
                {
                    var tipoNota = InferirTipoNotaPorCpf(cpfProdutor, item.CpfCnpjEmitente, item.CpfCnpjDestinatario);
                    if (tipoNota == null)
                    {
                        notasIgnoradas++;
                        continue;
                    }

                    var notaExistente = await notaRepository.GetByChaveAcessoAsync(item.ChaveAcesso);

                    var dataEmissao = item.DataEmissao ?? dataImportacao.Date;
                    var numeroNota = string.IsNullOrWhiteSpace(item.NumeroNota) ? "SEM_NUMERO" : item.NumeroNota.Trim();
                    var origem = string.IsNullOrWhiteSpace(item.RazaoSocialEmitente) ? "Não informado" : item.RazaoSocialEmitente.Trim();
                    var destino = string.IsNullOrWhiteSpace(item.RazaoSocialDestinatario) ? "Não informado" : item.RazaoSocialDestinatario.Trim();
                    var descricao = MontarDescricao(item);
                    var naturezaOperacao = string.IsNullOrWhiteSpace(item.NaturezaOperacao) ? null : item.NaturezaOperacao.Trim();
                    var cfops = item.Cfops.Count == 0 ? null : string.Join(",", item.Cfops.Distinct());
                    var itensDescricao = item.DescricoesProdutosServicos.Count == 0 ? null : string.Join(" | ", item.DescricoesProdutosServicos.Distinct());

                    if (SalvarHtmlConsultaSeDisponivel(diretorioHtmlConsulta, item))
                    {
                        htmlSalvos++;
                    }

                    if (notaExistente == null)
                    {
                        var novaNota = new NotaFiscal
                        {
                            ChaveAcesso = item.ChaveAcesso,
                            DataEmissao = dataEmissao,
                            NumeroDaNota = numeroNota,
                            ValorNotaFiscal = item.ValorTotal,
                            Origem = origem,
                            Destino = destino,
                            Descricao = descricao,
                            NaturezaOperacao = naturezaOperacao,
                            Cfops = cfops,
                            ItensDescricao = itensDescricao,
                            TipoNota = tipoNota.Value,
                            ProdutorId = produtorId
                        };

                        await notaRepository.AddAsync(novaNota);
                        novasNotas++;
                        continue;
                    }

                    notaExistente.DataEmissao = dataEmissao;
                    notaExistente.NumeroDaNota = numeroNota;
                    notaExistente.ValorNotaFiscal = item.ValorTotal;
                    notaExistente.Origem = origem;
                    notaExistente.Destino = destino;
                    notaExistente.Descricao = descricao;
                    notaExistente.NaturezaOperacao = naturezaOperacao;
                    notaExistente.Cfops = cfops;
                    notaExistente.ItensDescricao = itensDescricao;
                    notaExistente.TipoNota = tipoNota.Value;
                    notaExistente.ProdutorId = produtorId;

                    await notaRepository.UpdateAsync(notaExistente);
                    notasAtualizadas++;
                }

                Status = $"Importação finalizada. {chaves.Count} chave(s), {novasNotas} nota(s) nova(s), {notasAtualizadas} atualizada(s), {notasIgnoradas} ignorada(s) e {htmlSalvos} HTML(s) salvo(s) em {diretorioHtmlConsulta}.";
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

        private static string NormalizarSomenteDigitos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            return ApenasDigitosRegex.Replace(valor, string.Empty);
        }

        private static TipoNota? InferirTipoNotaPorCpf(string cpfProdutor, string? cpfCnpjEmitente, string? cpfCnpjDestinatario)
        {
            var emitenteNormalizado = NormalizarSomenteDigitos(cpfCnpjEmitente);
            var destinatarioNormalizado = NormalizarSomenteDigitos(cpfCnpjDestinatario);

            if (cpfProdutor == emitenteNormalizado)
            {
                return TipoNota.Saida;
            }

            if (cpfProdutor == destinatarioNormalizado)
            {
                return TipoNota.Entrada;
            }

            return null;
        }

        private static string MontarDescricao(SefazNFeCapturada item)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(item.Situacao))
            {
                partes.Add($"Situação: {item.Situacao.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(item.NaturezaOperacao))
            {
                partes.Add($"Natureza: {item.NaturezaOperacao.Trim()}");
            }

            if (item.Cfops.Count > 0)
            {
                var cfops = string.Join(", ", item.Cfops.Distinct());
                partes.Add($"CFOP(s): {cfops}");
            }

            if (item.DescricoesProdutosServicos.Count > 0)
            {
                var itens = string.Join(" | ", item.DescricoesProdutosServicos.Distinct());
                partes.Add($"Itens: {itens}");
            }

            if (partes.Count == 0)
            {
                partes.Add("Importado automaticamente da SEFAZ-MS");
            }

            var descricao = string.Join(". ", partes);
            if (descricao.Length > 500)
            {
                return descricao.Substring(0, 500);
            }

            return descricao;
        }

        private static string CriarDiretorioConsultaHtml(string cpfProdutor)
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoLCPR",
                "consultas-nfe",
                cpfProdutor);

            Directory.CreateDirectory(basePath);
            return basePath;
        }

        private static bool SalvarHtmlConsultaSeDisponivel(string diretorio, SefazNFeCapturada item)
        {
            if (string.IsNullOrWhiteSpace(item.HtmlConsulta) || string.IsNullOrWhiteSpace(item.ChaveAcesso))
            {
                return false;
            }

            try
            {
                var nomeArquivo = $"{item.ChaveAcesso}.html";
                var filePath = Path.Combine(diretorio, nomeArquivo);
                File.WriteAllText(filePath, item.HtmlConsulta);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
