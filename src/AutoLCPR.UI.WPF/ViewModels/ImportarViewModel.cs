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
using AutoLCPR.Application.Services;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.UI.WPF.ViewModels
{
    public class ImportarViewModel : INotifyPropertyChanged
    {
        public const string SefazUrl = "http://eservicos.sefaz.ms.gov.br/";
        private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);

        private readonly IServiceProvider? _serviceProvider;
        private readonly ImportacaoContextoService? _contextoImportacao;
        private string _status = "Abra o site no painel para iniciar.";
        private string _progressTitle = "Progresso";
        private string _progressMessage = string.Empty;
        private double _progressValue;
        private double _progressMaximum = 1d;
        private bool _isProgressVisible;
        private bool _isProgressIndeterminate;
        private bool _isImportando;
        private DateTime _dataInicio;
        private DateTime _dataFim;

        public ObservableCollection<string> ChavesImportadas { get; } = new();
        public Func<Task<IReadOnlyList<SefazNFeCapturada>>>? CapturarNotasNoNavegadorAsync { get; set; }

        public ICommand ExecutarImportacaoCommand { get; }
        public ICommand ImportarNotasCommand { get; }

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

        public string ProgressTitle
        {
            get => _progressTitle;
            private set
            {
                if (_progressTitle != value)
                {
                    _progressTitle = value;
                    OnPropertyChanged(nameof(ProgressTitle));
                }
            }
        }

        public string ProgressMessage
        {
            get => _progressMessage;
            private set
            {
                if (_progressMessage != value)
                {
                    _progressMessage = value;
                    OnPropertyChanged(nameof(ProgressMessage));
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            private set
            {
                if (Math.Abs(_progressValue - value) > 0.001d)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                    OnPropertyChanged(nameof(ProgressSummary));
                }
            }
        }

        public double ProgressMaximum
        {
            get => _progressMaximum;
            private set
            {
                var normalized = value <= 0 ? 1d : value;
                if (Math.Abs(_progressMaximum - normalized) > 0.001d)
                {
                    _progressMaximum = normalized;
                    OnPropertyChanged(nameof(ProgressMaximum));
                    OnPropertyChanged(nameof(ProgressSummary));
                }
            }
        }

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            private set
            {
                if (_isProgressVisible != value)
                {
                    _isProgressVisible = value;
                    OnPropertyChanged(nameof(IsProgressVisible));
                }
            }
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            private set
            {
                if (_isProgressIndeterminate != value)
                {
                    _isProgressIndeterminate = value;
                    OnPropertyChanged(nameof(IsProgressIndeterminate));
                    OnPropertyChanged(nameof(ProgressSummary));
                }
            }
        }

        public string ProgressSummary
        {
            get
            {
                if (!IsProgressVisible)
                {
                    return string.Empty;
                }

                if (IsProgressIndeterminate)
                {
                    return "Em andamento";
                }

                var total = Math.Max(1d, ProgressMaximum);
                var atual = Math.Min(total, Math.Max(0d, ProgressValue));
                var percentual = atual / total * 100d;
                return $"{atual:0}/{total:0} ({percentual:0}%)";
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
            ImportarNotasCommand = new RelayCommand(() => _ = ImportarNotas(), () => !IsImportando);
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
                Status = "Nenhum produtor selecionado. Selecione um produtor na Dashboard antes de capturar.";
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
            AplicarProgresso(new ImportacaoProgresso
            {
                Etapa = "Captura de NF-es",
                Mensagem = "Preparando captura no portal da SEFAZ...",
                Indeterminado = true
            });

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var produtorRepository = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                var nfeImportService = scope.ServiceProvider.GetRequiredService<NfeImportService>();

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
                var produtor = await produtorRepository.GetByIdAsync(produtorId);

                if (produtor == null)
                {
                    Status = "Produtor selecionado não encontrado.";
                    return;
                }

                var cpfProdutor = NormalizarSomenteDigitos(produtor.Cpf);
                if (cpfProdutor.Length != 11)
                {
                    Status = "CPF do produtor selecionado não está válido. Atualize o cadastro do produtor antes de capturar.";
                    return;
                }

                var diretorioHtmlConsulta = await nfeImportService.CriarPastaProdutorAsync(cpfProdutor);
                var htmlSalvos = 0;

                var notasDistintas = notasCapturadas.GroupBy(item => item.ChaveAcesso).Select(item => item.First()).ToList();
                AplicarProgresso(new ImportacaoProgresso
                {
                    Etapa = "Captura de NF-es",
                    Mensagem = "Salvando HTMLs capturados...",
                    Atual = 0,
                    Total = notasDistintas.Count,
                    Indeterminado = notasDistintas.Count == 0
                });

                for (var i = 0; i < notasDistintas.Count; i++)
                {
                    if (await SalvarHtmlConsultaSeDisponivelAsync(diretorioHtmlConsulta, notasDistintas[i]))
                    {
                        htmlSalvos++;
                    }

                    AplicarProgresso(new ImportacaoProgresso
                    {
                        Etapa = "Captura de NF-es",
                        Mensagem = $"Salvando HTMLs capturados {i + 1}/{notasDistintas.Count}...",
                        Atual = i + 1,
                        Total = notasDistintas.Count,
                        Indeterminado = false
                    });

                    if ((i + 1) % 10 == 0)
                    {
                        await Task.Yield();
                    }
                }

                Status = $"Captura finalizada. {chaves.Count} NF-e(s) capturada(s) e {htmlSalvos} arquivo(s) HTML salvo(s) em {diretorioHtmlConsulta}.";
            }
            catch (Exception ex)
            {
                Status = $"Erro durante captura: {ex.Message}";
                MessageBox.Show(Status, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LimparProgresso();
                IsImportando = false;
            }
        }

        public void AplicarProgresso(ImportacaoProgresso progresso)
        {
            ProgressTitle = string.IsNullOrWhiteSpace(progresso.Etapa) ? "Progresso" : progresso.Etapa;
            ProgressMessage = progresso.Mensagem ?? string.Empty;
            ProgressMaximum = progresso.Total <= 0 ? 1d : progresso.Total;
            ProgressValue = Math.Max(0d, Math.Min(progresso.Atual, ProgressMaximum));
            IsProgressIndeterminate = progresso.Indeterminado;
            IsProgressVisible = true;
            OnPropertyChanged(nameof(ProgressSummary));
        }

        public void LimparProgresso()
        {
            ProgressTitle = "Progresso";
            ProgressMessage = string.Empty;
            ProgressMaximum = 1d;
            ProgressValue = 0d;
            IsProgressIndeterminate = false;
            IsProgressVisible = false;
            OnPropertyChanged(nameof(ProgressSummary));
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

        private static async Task<bool> SalvarHtmlConsultaSeDisponivelAsync(string diretorio, SefazNFeCapturada item)
        {
            if (string.IsNullOrWhiteSpace(item.HtmlConsulta) || string.IsNullOrWhiteSpace(item.ChaveAcesso))
            {
                return false;
            }

            try
            {
                var nomeArquivo = $"{item.ChaveAcesso}.html";
                var filePath = Path.Combine(diretorio, nomeArquivo);
                await File.WriteAllTextAsync(filePath, item.HtmlConsulta);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ImportarNotas()
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

            IsImportando = true;
            Status = "Importando notas fiscais dos arquivos HTML...";
            AplicarProgresso(new ImportacaoProgresso
            {
                Etapa = "Importação de NF-es",
                Mensagem = "Preparando importação e validando arquivos...",
                Indeterminado = true
            });

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var produtorRepository = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                var notaFiscalRepository = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                var lancamentoRepository = scope.ServiceProvider.GetRequiredService<ILancamentoRepository>();
                var nfeImportService = scope.ServiceProvider.GetRequiredService<NfeImportService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var produtorId = _contextoImportacao.ProdutorSelecionadoId.Value;
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

                // Limpar importações anteriores do produtor para reprocessar com dados corretos
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Lancamentos WHERE ProdutorId = {produtorId}");
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM NotasFiscais WHERE ProdutorId = {produtorId}");

                // Importar notas usando o NfeImportService
                var progressoLeitura = new Progress<ImportacaoProgresso>(AplicarProgresso);
                var notasDto = await nfeImportService.ImportarNotasAsync(cpfProdutor, produtorId, progressoLeitura);

                if (notasDto.Count == 0)
                {
                    Status = "Nenhuma nota fiscal encontrada para importar. Execute a captura primeiro.";
                    return;
                }

                int notasImportadas = 0;
                int notasJaExistiam = 0;
                int lancamentosCriados = 0;
                var arquivosHtml = Directory.GetFiles(await nfeImportService.ObterCaminoPastaProdutorAsync(cpfProdutor), "*.html").Length;
                var notasIgnoradasPorRegra = arquivosHtml - notasDto.Count;

                AplicarProgresso(new ImportacaoProgresso
                {
                    Etapa = "Importação de NF-es",
                    Mensagem = "Gravando notas e lançamentos no banco...",
                    Atual = 0,
                    Total = notasDto.Count,
                    Indeterminado = false
                });

                for (var i = 0; i < notasDto.Count; i++)
                {
                    var notaDto = notasDto[i];
                    AplicarProgresso(new ImportacaoProgresso
                    {
                        Etapa = "Importação de NF-es",
                        Mensagem = $"Processando nota {i + 1}/{notasDto.Count}...",
                        Atual = i,
                        Total = notasDto.Count,
                        Indeterminado = false
                    });

                    // Verificar se a nota já existe no banco
                    var notaExistente = await notaFiscalRepository.GetByChaveAcessoAsync(notaDto.Chave);
                    if (notaExistente != null)
                    {
                        notasJaExistiam++;
                        AplicarProgresso(new ImportacaoProgresso
                        {
                            Etapa = "Importação de NF-es",
                            Mensagem = $"Processando nota {i + 1}/{notasDto.Count}...",
                            Atual = i + 1,
                            Total = notasDto.Count,
                            Indeterminado = false
                        });
                        continue;
                    }

                    // Identificar se o produtor é emitente ou destinatário
                    var produtorEhEmitente = cpfProdutor == notaDto.EmitenteCpfCnpj;
                    var produtorEhDestinatario = cpfProdutor == notaDto.DestinatarioCpfCnpj;

                    // Determinar origem e destino baseado em quem é o produtor E o tipo classificado
                    // REGRA ESPECIAL: Se há conflito entre tipo classificado e posição do produtor,
                    // ajustar para mostrar a contraparte correta
                    string origem, destino, clienteFornecedor;
                    
                    // Verificar se há conflito entre tipo e posição
                    var temConflito = (notaDto.Tipo == TipoLancamento.Receita && produtorEhDestinatario) ||
                                      (notaDto.Tipo == TipoLancamento.Despesa && produtorEhEmitente);
                    
                    if (temConflito && produtorEhDestinatario && notaDto.Tipo == TipoLancamento.Receita)
                    {
                        // CONFLITO: Classificado como RECEITA mas produtor é DESTINATÁRIO
                        // Neste caso, o EMITENTE é a contraparte (fornecedor que vendeu)
                        origem = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                        destino = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                        clienteFornecedor = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                    }
                    else if (temConflito && produtorEhEmitente && notaDto.Tipo == TipoLancamento.Despesa)
                    {
                        // CONFLITO: Classificado como DESPESA mas produtor é EMITENTE
                        // Neste caso, o DESTINATÁRIO é a contraparte (cliente que comprou)
                        origem = LimitarTexto(produtor.Nome, 200, notaDto.EmitenteNome);
                        destino = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                        clienteFornecedor = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                    }
                    else if (produtorEhEmitente)
                    {
                        // SEM CONFLITO: Produtor é o emitente (nota de SAÍDA/RECEITA normal)
                        // Cliente está comprando do produtor
                        origem = LimitarTexto(produtor.Nome, 200, notaDto.EmitenteNome);
                        destino = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                        clienteFornecedor = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                    }
                    else if (produtorEhDestinatario)
                    {
                        // SEM CONFLITO: Produtor é o destinatário (nota de ENTRADA/DESPESA normal)
                        // Produtor está comprando do fornecedor
                        origem = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                        destino = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                        clienteFornecedor = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                    }
                    else
                    {
                        // Caso especial: produtor não é nem emitente nem destinatário
                        // Manter dados originais
                        origem = LimitarTexto(notaDto.EmitenteNome, 200, "N/D");
                        destino = LimitarTexto(notaDto.DestinatarioNome, 200, "N/D");
                        clienteFornecedor = LimitarTexto(
                            notaDto.Tipo == TipoLancamento.Receita ? notaDto.DestinatarioNome : notaDto.EmitenteNome, 
                            200, 
                            "NFe"
                        );
                    }

                    // Criar entidade NotaFiscal
                    var notaFiscal = new NotaFiscal
                    {
                        ChaveAcesso = notaDto.Chave,
                        ProdutorId = produtorId,
                        TipoNota = notaDto.Tipo == TipoLancamento.Receita ? TipoNota.Saida : TipoNota.Entrada,
                        NumeroDaNota = LimitarTexto(notaDto.NumeroNota, 20, notaDto.Chave.Substring(Math.Max(0, notaDto.Chave.Length - 9))),
                        DataEmissao = notaDto.DataEmissao ?? DateTime.Now,
                        ValorNotaFiscal = notaDto.ValorTotal,
                        Origem = origem,
                        Destino = destino,
                        Descricao = LimitarTexto(notaDto.Descricao, 500, "Importado automaticamente da SEFAZ-MS"),
                        NaturezaOperacao = LimitarTexto(notaDto.Natureza, 1000),
                        Cfops = LimitarTexto(notaDto.CFOP, 500),
                        ItensDescricao = LimitarTexto(notaDto.Descricao, 2000)
                    };

                    await notaFiscalRepository.AddAsync(notaFiscal);
                    notasImportadas++;

                    // Criar Lançamento correspondente
                    var lancamento = new Lancamento
                    {
                        Tipo = notaDto.Tipo,
                        ProdutorId = produtorId,
                        ClienteFornecedor = clienteFornecedor,
                        Descricao = LimitarTexto($"{notaDto.Descricao} - NF {notaFiscal.NumeroDaNota}", 500, "Lançamento importado de NF-e"),
                        Situacao = "Confirmado",
                        Valor = notaDto.ValorTotal,
                        Data = notaFiscal.DataEmissao,
                        Vencimento = notaFiscal.DataEmissao
                    };

                    await lancamentoRepository.AddAsync(lancamento);
                    lancamentosCriados++;

                    AplicarProgresso(new ImportacaoProgresso
                    {
                        Etapa = "Importação de NF-es",
                        Mensagem = $"Processando nota {i + 1}/{notasDto.Count}...",
                        Atual = i + 1,
                        Total = notasDto.Count,
                        Indeterminado = false
                    });

                    if ((i + 1) % 10 == 0)
                    {
                        await Task.Yield();
                    }
                }

                // Montar mensagem detalhada
                var mensagem = new System.Text.StringBuilder();
                mensagem.AppendLine($"✓ Arquivos HTML encontrados: {arquivosHtml}");
                mensagem.AppendLine($"✓ Notas processadas com sucesso: {notasImportadas}");
                mensagem.AppendLine($"✓ Lançamentos criados: {lancamentosCriados}");
                
                if (notasJaExistiam > 0)
                {
                    mensagem.AppendLine($"\n⚠ Notas já existentes (ignoradas): {notasJaExistiam}");
                }
                
                if (notasIgnoradasPorRegra > 0)
                {
                    mensagem.AppendLine($"\n⊗ Notas ignoradas por regras de configuração: {notasIgnoradasPorRegra}");
                    mensagem.AppendLine("  (CFOPs ou Naturezas nas listas de exclusão)");
                }
                
                Status = $"Importação concluída! {notasImportadas} nota(s) importada(s).";
                MessageBox.Show(mensagem.ToString(), "Importação Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var detalhe = ex.InnerException?.Message;
                Status = string.IsNullOrWhiteSpace(detalhe)
                    ? $"Erro durante importação: {ex.Message}"
                    : $"Erro durante importação: {detalhe}";
                MessageBox.Show(Status, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LimparProgresso();
                IsImportando = false;
            }
        }

        private static string LimitarTexto(string? valor, int tamanhoMaximo, string fallback = "")
        {
            var texto = string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
            if (texto.Length <= tamanhoMaximo)
            {
                return texto;
            }

            return texto.Substring(0, tamanhoMaximo);
        }
    }
}
