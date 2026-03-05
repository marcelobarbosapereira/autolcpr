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

                foreach (var item in notasCapturadas.GroupBy(item => item.ChaveAcesso).Select(item => item.First()))
                {
                    if (SalvarHtmlConsultaSeDisponivel(diretorioHtmlConsulta, item))
                    {
                        htmlSalvos++;
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
                var notasDto = await nfeImportService.ImportarNotasAsync(cpfProdutor, produtorId);

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

                foreach (var notaDto in notasDto)
                {
                    // Verificar se a nota já existe no banco
                    var notaExistente = await notaFiscalRepository.GetByChaveAcessoAsync(notaDto.Chave);
                    if (notaExistente != null)
                    {
                        notasJaExistiam++;
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
