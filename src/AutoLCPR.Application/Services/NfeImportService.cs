using System.Text.RegularExpressions;
using System.Globalization;
using HtmlAgilityPack;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Application.DTOs;

namespace AutoLCPR.Application.Services
{
    /// <summary>
    /// Serviço para importar e processar Notas Fiscais do HTML
    /// </summary>
    public class NfeImportService
    {
        private readonly NfeConfigService _configService;

        public NfeImportService(NfeConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Importa todas as notas fiscais da pasta do produtor
        /// Estrutura: {pastaRaiz}/{cpfProdutor}
        /// </summary>
        public async Task<List<NotaFiscalDTO>> ImportarNotasAsync(string cpfProdutor, int? produtorId = null)
        {
            if (string.IsNullOrWhiteSpace(cpfProdutor))
            {
                throw new ArgumentException("CPF do produtor não foi informado.", nameof(cpfProdutor));
            }

            var config = await _configService.CarregarConfiguracaoAsync();
            var notas = new List<NotaFiscalDTO>();

            // Expandir variáveis de ambiente
            var pastaRaiz = Environment.ExpandEnvironmentVariables(config.PastaHtml);
            // Normalizar separadores de caminho para o sistema operacional
            pastaRaiz = Path.GetFullPath(pastaRaiz);
            
            // Construir caminho da subpasta do produtor
            var pastaProdutoror = Path.Combine(pastaRaiz, cpfProdutor.Trim());

            // Criar pasta se não existir
            try
            {
                if (!Directory.Exists(pastaProdutoror))
                {
                    Directory.CreateDirectory(pastaProdutoror);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao criar/acessar pasta do produtor: {pastaProdutoror}", ex);
            }

            // Buscar todos os arquivos HTML da subpasta do produtor
            var arquivosHtml = Directory.GetFiles(pastaProdutoror, "*.html", SearchOption.TopDirectoryOnly);

            if (arquivosHtml.Length == 0)
            {
                // Pasta vazia, retornar lista vazia (não é erro)
                throw new Exception($"Nenhum arquivo HTML encontrado em: {pastaProdutoror}");
            }

            foreach (var arquivo in arquivosHtml)
            {
                try
                {
                    var nota = ProcessarArquivoHtml(arquivo, config, produtorId);
                    if (nota != null)
                    {
                        notas.Add(nota);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar arquivo {arquivo}: {ex.Message}");
                    // Continuar processando outros arquivos
                }
            }

            if (notas.Count == 0 && arquivosHtml.Length > 0)
            {
                throw new Exception($"Foram encontrados {arquivosHtml.Length} arquivo(s) HTML, mas nenhum pôde ser processado com sucesso. Verifique o formato dos arquivos.");
            }

            return notas;
        }

        /// <summary>
        /// Obtém o caminho da pasta de um produtor específico
        /// </summary>
        public async Task<string> ObterCaminoPastaProdutorAsync(string cpfProdutor)
        {
            if (string.IsNullOrWhiteSpace(cpfProdutor))
            {
                throw new ArgumentException("CPF do produtor não foi informado.", nameof(cpfProdutor));
            }

            var config = await _configService.CarregarConfiguracaoAsync();
            var pastaRaiz = Environment.ExpandEnvironmentVariables(config.PastaHtml);
            // Normalizar separadores de caminho para o sistema operacional
            pastaRaiz = Path.GetFullPath(pastaRaiz);
            return Path.Combine(pastaRaiz, cpfProdutor.Trim());
        }

        /// <summary>
        /// Cria a pasta de um produtor se ela não existir
        /// </summary>
        public async Task<string> CriarPastaProdutorAsync(string cpfProdutor)
        {
            var caminho = await ObterCaminoPastaProdutorAsync(cpfProdutor);
            
            if (!Directory.Exists(caminho))
            {
                Directory.CreateDirectory(caminho);
            }

            return caminho;
        }

        /// <summary>
        /// Processa um arquivo HTML e retorna a nota fiscal se passar nas validações
        /// </summary>
        private NotaFiscalDTO? ProcessarArquivoHtml(string caminhoArquivo, NfeImportConfig config, int? produtorId)
        {
            var html = new HtmlDocument();
            html.Load(caminhoArquivo);

            // Extrair dados do HTML
            var chave = ExtrairChaveAcesso(html);
            var natureza = ExtrairNaturezaOperacao(html);
            var numeroNota = ExtrairNumeroNota(html);
            var dataEmissao = ExtrairDataEmissao(html);
            var emitenteNome = ObterTextoPorId(html, "cphConteudoPrincipal_LBLIde_nome_fantasia");
            var emitenteCpfCnpj = NormalizarSomenteDigitos(ObterTextoPorId(html, "cphConteudoPrincipal_LBLIde_cnpj"));
            var destinatarioNome = ObterTextoPorId(html, "cphConteudoPrincipal_LBLIdd_razao_social");
            var destinatarioCpfCnpj = NormalizarSomenteDigitos(ObterTextoPorId(html, "cphConteudoPrincipal_LBLIdd_cnpj"));
            var itens = ExtrairItens(html);
            var valorTotal = ExtrairValorTotal(html);

            if (valorTotal <= 0 && itens.Count > 0)
            {
                valorTotal = ExtrairValorTotalDosItens(html);
            }

            // Se não tiver itens, ignorar
            if (itens.Count == 0)
            {
                return null;
            }

            // REGRA 1: Verificar se CFOP está em IgnorarCFOP
            foreach (var cfop in itens.Select(i => i.CFOP).Distinct())
            {
                if (config.IgnorarCFOP.Any(c => c.Equals(cfop, StringComparison.OrdinalIgnoreCase)))
                {
                    return null; // Ignorar esta nota
                }
            }

            // REGRA 2: Verificar se Natureza está em IgnorarNatureza
            if (!string.IsNullOrEmpty(natureza))
            {
                if (config.IgnorarNatureza.Any(n => 
                    natureza.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    return null; // Ignorar esta nota
                }
            }

            // Concatenar descrições (primeiras duas palavras de cada item)
            var descricao = ConcatenarDescricoes(itens);

            // Concatenar CFOPs
            var cfopsStr = string.Join(",", itens.Select(i => i.CFOP).Distinct());

            // Determinar o tipo de lançamento
            var tipo = DeterminarTipoLancamento(config, itens, natureza, produtorId);

            var nota = new NotaFiscalDTO
            {
                Chave = chave,
                Natureza = natureza,
                Descricao = descricao,
                CFOP = cfopsStr,
                NumeroNota = numeroNota,
                DataEmissao = dataEmissao,
                EmitenteNome = emitenteNome,
                EmitenteCpfCnpj = emitenteCpfCnpj,
                DestinatarioNome = destinatarioNome,
                DestinatarioCpfCnpj = destinatarioCpfCnpj,
                ValorTotal = valorTotal,
                Tipo = tipo
            };

            return nota;
        }

        /// <summary>
        /// Extrai a chave de acesso do HTML
        /// </summary>
        private string ExtrairChaveAcesso(HtmlDocument html)
        {
            var chavePorId = ObterTextoPorId(html, "cphConteudoPrincipal_lblNfe_Chave_acesso");
            if (!string.IsNullOrWhiteSpace(chavePorId) && Regex.IsMatch(chavePorId, @"^\d{44}$"))
            {
                return chavePorId;
            }

            // Procurar por chave com 44 dígitos
            var regex = new Regex(@"\d{44}", RegexOptions.IgnoreCase);
            var body = html.DocumentNode.InnerText;
            var match = regex.Match(body);
            return match.Success ? match.Value : string.Empty;
        }

        /// <summary>
        /// Extrai a natureza da operação do HTML
        /// </summary>
        private string ExtrairNaturezaOperacao(HtmlDocument html)
        {
            var naturezaPorId = ObterTextoPorId(html, "cphConteudoPrincipal_LBLNfe_natureza_operacao");
            if (!string.IsNullOrWhiteSpace(naturezaPorId))
            {
                return naturezaPorId;
            }

            return string.Empty;
        }

        private string ExtrairNumeroNota(HtmlDocument html)
        {
            var numero = ObterTextoPorId(html, "cphConteudoPrincipal_LBLNfe_numero");
            return Regex.Replace(numero, @"\D", string.Empty);
        }

        private DateTime? ExtrairDataEmissao(HtmlDocument html)
        {
            var texto = ObterTextoPorId(html, "cphConteudoPrincipal_LBLNfe_data_emissao");
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            var match = Regex.Match(texto, @"\d{2}/\d{2}/\d{4}");
            if (!match.Success)
            {
                return null;
            }

            if (DateTime.TryParseExact(match.Value, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Extrai os itens da nota (descrição e CFOP)
        /// </summary>
        private List<(string Descricao, string CFOP)> ExtrairItens(HtmlDocument html)
        {
            var itens = new List<(string, string)>();

            try
            {
                var tabelaItens = html.GetElementbyId("cphConteudoPrincipal_GVWItens");
                if (tabelaItens == null)
                {
                    return itens;
                }

                var linhas = tabelaItens.SelectNodes(".//tr");
                if (linhas == null)
                {
                    return itens;
                }

                foreach (var linha in linhas)
                {
                    var colunas = linha.SelectNodes("./td");
                    if (colunas == null || colunas.Count < 5)
                    {
                        continue;
                    }

                    var descricao = LimparTexto(colunas[1].InnerText);
                    var cfop = Regex.Match(LimparTexto(colunas[4].InnerText), @"\d{4}").Value;

                    if (descricao.Contains("DESCRIÇÃO DO PRODUTO", StringComparison.OrdinalIgnoreCase) ||
                        descricao.Contains("PÁGINA", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(descricao) && !string.IsNullOrWhiteSpace(cfop))
                    {
                        itens.Add((descricao, cfop));
                    }
                }
            }
            catch
            {
                // Se houver erro ao processar tabelas, continuar
            }

            return itens;
        }

        /// <summary>
        /// Extrai o valor total da nota
        /// </summary>
        private decimal ExtrairValorTotal(HtmlDocument html)
        {
            try
            {
                var porId = ObterTextoPorId(html, "cphConteudoPrincipal_LBLNfe_vlr_total");
                if (TryParseDecimalPtBr(porId, out var valorPorId))
                {
                    return valorPorId;
                }
            }
            catch
            {
                // Se houver erro, retornar 0
            }

            return 0m;
        }

        private decimal ExtrairValorTotalDosItens(HtmlDocument html)
        {
            try
            {
                var tabelaItens = html.GetElementbyId("cphConteudoPrincipal_GVWItens");
                if (tabelaItens == null)
                {
                    return 0m;
                }

                var linhas = tabelaItens.SelectNodes(".//tr");
                if (linhas == null)
                {
                    return 0m;
                }

                decimal total = 0m;
                foreach (var linha in linhas)
                {
                    var colunas = linha.SelectNodes("./td");
                    if (colunas == null || colunas.Count < 9)
                    {
                        continue;
                    }

                    var valorTotalItemTexto = LimparTexto(colunas[8].InnerText);
                    if (TryParseDecimalPtBr(valorTotalItemTexto, out var valorItem))
                    {
                        total += valorItem;
                    }
                }

                return total;
            }
            catch
            {
                return 0m;
            }
        }

        /// <summary>
        /// Concatena as descrições dos itens (primeiras duas palavras)
        /// </summary>
        private string ConcatenarDescricoes(List<(string Descricao, string CFOP)> itens)
        {
            var descricoes = itens
                .Select(i => LimparTexto(i.Descricao))
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .Take(3)
                .ToList();

            return string.Join(" | ", descricoes);
        }

        private static string ObterTextoPorId(HtmlDocument html, string id)
        {
            var node = html.GetElementbyId(id);
            return LimparTexto(node?.InnerText);
        }

        private static string LimparTexto(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            return Regex.Replace(texto, @"\s+", " ").Trim();
        }

        private static bool TryParseDecimalPtBr(string? texto, out decimal valor)
        {
            valor = 0m;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            var apenasNumero = Regex.Replace(texto, @"[^0-9,.-]", string.Empty);
            return decimal.TryParse(apenasNumero, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out valor);
        }

        /// <summary>
        /// Determina o tipo de lançamento baseado nas regras configuradas
        /// </summary>
        private TipoLancamento DeterminarTipoLancamento(
            NfeImportConfig config,
            List<(string Descricao, string CFOP)> itens,
            string natureza,
            int? produtorId)
        {
            // REGRA 3: Verificar CFOPReceita
            foreach (var cfop in itens.Select(i => i.CFOP).Distinct())
            {
                if (config.CFOPReceita.Any(c => c.Equals(cfop, StringComparison.OrdinalIgnoreCase)))
                {
                    return TipoLancamento.Receita;
                }
            }

            // REGRA 4: Verificar CFOPDespesa
            foreach (var cfop in itens.Select(i => i.CFOP).Distinct())
            {
                if (config.CFOPDespesa.Any(c => c.Equals(cfop, StringComparison.OrdinalIgnoreCase)))
                {
                    return TipoLancamento.Despesa;
                }
            }

            // REGRA 5: Verificar NaturezaReceita
            if (!string.IsNullOrEmpty(natureza))
            {
                if (config.NaturezaReceita.Any(n => 
                    natureza.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    return TipoLancamento.Receita;
                }
            }

            // REGRA 6: Verificar NaturezaDespesa
            if (!string.IsNullOrEmpty(natureza))
            {
                if (config.NaturezaDespesa.Any(n => 
                    natureza.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    return TipoLancamento.Despesa;
                }
            }

            // Lógica padrão: se emitente == produtor, é receita, senão é despesa
            // Por enquanto, retornar DESPESA como padrão (não temos acesso ao CNPJ do emitente aqui)
            return TipoLancamento.Despesa;
        }

        /// <summary>
        /// Normaliza CPF/CNPJ removendo caracteres não numéricos
        /// </summary>
        private static string NormalizarSomenteDigitos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            return Regex.Replace(valor, @"\D", string.Empty);
        }
    }
}
