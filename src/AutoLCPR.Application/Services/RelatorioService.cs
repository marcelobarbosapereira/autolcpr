using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.Application.Services
{
    /// <summary>
    /// Serviço para gerar relatórios em PDF baseado em templates HTML
    /// </summary>
    public class RelatorioService
    {
        private readonly string _templatePath;
        private readonly IServiceProvider _serviceProvider;

        public RelatorioService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _templatePath = EncontrarPastaTemplates();
        }

        /// <summary>
        /// Encontra a pasta de templates procurando em múltiplos locais possíveis
        /// </summary>
        private string EncontrarPastaTemplates()
        {
            // Tentar 1: Diretório de execução + Relatorios
            var caminho1 = Path.Combine(AppContext.BaseDirectory, "Relatorios");
            if (Directory.Exists(caminho1))
                return caminho1;

            // Tentar 2: Navegar até a raiz do projeto e procurar em src/AutoLCPR.Application/Relatorios
            var rootDir = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++) // Procurar até 10 níveis acima
            {
                rootDir = Directory.GetParent(rootDir)?.FullName;
                if (rootDir == null)
                    break;

                var caminho = Path.Combine(rootDir, "src", "AutoLCPR.Application", "Relatorios");
                if (Directory.Exists(caminho))
                    return caminho;
            }

            // Se não encontrar, retornar o padrão (will fail with better error message)
            return Path.Combine(AppContext.BaseDirectory, "Relatorios");
        }

        /// <summary>
        /// Gera o relatório do Livro Caixa em PDF
        /// </summary>
        public async Task<string> GerarLivroCaixaAsync(int anoBase, int? produtorId = null)
        {
            try
            {
                // Carregar o template HTML
                var templatePath = Path.Combine(_templatePath, "Template-LivroCaixa.HTML");
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template não encontrado: {templatePath}");
                }

                var templateContent = await File.ReadAllTextAsync(templatePath);

                // Obter os dados para preencher o template
                var htmlContent = await PreencherTemplateAsync(templateContent, anoBase, produtorId);

                // Gerar o PDF (implementado no WPF com Playwright)
                var pastaRelatios = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AutoLCPR",
                    "Relatorios"
                );

                Directory.CreateDirectory(pastaRelatios);

                var caminhoSaida = Path.Combine(
                    pastaRelatios,
                    $"LivroCaixa_{anoBase}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                );

                // Salvar o HTML processado temporariamente
                var caminhoHtmlTemp = Path.Combine(pastaRelatios, $"temp_{DateTime.Now.Ticks}.html");
                await File.WriteAllTextAsync(caminhoHtmlTemp, htmlContent);

                return caminhoHtmlTemp;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao gerar relatório: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Preenche o template HTML com os dados do banco de dados
        /// </summary>
        private async Task<string> PreencherTemplateAsync(string templateContent, int anoBase, int? produtorId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notaRepo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                var produtorRepo = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();

                // anoBase é na verdade o exercício
                // O ano real dos dados é o ano anterior
                var anoExercicio = anoBase;
                var anoDados = anoBase - 1;

                // Carregar produtor
                Produtor? produtor = null;
                if (produtorId.HasValue)
                {
                    produtor = await produtorRepo.GetByIdAsync(produtorId.Value);
                }

                var nomeProduto = produtor?.Nome ?? "Relatório Geral";

                // Carregar notas fiscais do ano anterior (dados)
                var notas = new List<NotaFiscal>();
                if (produtorId.HasValue)
                {
                    var notasProdutor = await notaRepo.GetByProdutorIdAsync(produtorId.Value);
                    notas = notasProdutor.Where(n => n.DataEmissao.Year == anoDados).ToList();
                }
                else
                {
                    // Se não especificar produtor, carregar todas as notas do ano
                    var todasNotas = await notaRepo.GetAllAsync();
                    notas = todasNotas.Where(n => n.DataEmissao.Year == anoDados).ToList();
                }

                // Separar receitas e despesas
                var receitas = notas.Where(n => n.TipoNota == TipoNota.Saida).OrderBy(n => n.DataEmissao).ToList();
                var despesas = notas.Where(n => n.TipoNota == TipoNota.Entrada).OrderBy(n => n.DataEmissao).ToList();

                // Calcular totais
                var totalReceitas = receitas.Sum(n => n.ValorNotaFiscal);
                var totalDespesas = despesas.Sum(n => n.ValorNotaFiscal);
                var totalMargem = totalReceitas - totalDespesas;

                // Gerar linhas do relatório mensal
                var linhas = GerarLinhasRelatoriMensalAsync(receitas, despesas);

                // Gerar linhas de receitas detalhadas
                var linhasReceitas = GerarLinhasDetalhadas(receitas);

                // Gerar linhas de despesas detalhadas
                var linhasDespesas = GerarLinhasDetalhadas(despesas);

                // Calcular percentuais para gráfico
                var totalGeral = totalReceitas + totalDespesas;
                var percReceitas = totalGeral > 0 ? (totalReceitas / totalGeral * 100) : 0;
                var percDespesas = totalGeral > 0 ? (totalDespesas / totalGeral * 100) : 0;
                var percMargem = totalGeral > 0 ? (Math.Abs(totalMargem) / totalGeral * 100) : 0;

                // Carregar e embutir o CSS
                var cssPath = Path.Combine(_templatePath, "styles.css");
                var cssContent = File.Exists(cssPath) ? await File.ReadAllTextAsync(cssPath) : "";
                var cssTag = string.IsNullOrEmpty(cssContent) ? "" : $"<style>{cssContent}</style>";

                // Converter caminho da imagem para absolute file:// URL
                var headerImgPath = Path.Combine(_templatePath, "header.img");
                var headerImgUrl = File.Exists(headerImgPath) ? $"file:///{headerImgPath.Replace("\\", "/")}" : "";

                // Preencher o template
                var resultado = templateContent
                    .Replace("<meta charset=\"UTF-8\" />", $"<meta charset=\"UTF-8\" />\n  {cssTag}")
                    .Replace("src=\"header.img\"", $"src=\"{headerImgUrl}\"")
                    .Replace("{{nome}}", nomeProduto)
                    .Replace("{{anoBase}}", anoDados.ToString())
                    .Replace("{{exercicio}}", anoExercicio.ToString())
                    .Replace("{{totalReceitas}}", FormatarMoeda(totalReceitas))
                    .Replace("{{totalDespesas}}", FormatarMoeda(totalDespesas))
                    .Replace("{{totalMargem}}", FormatarMoeda(totalMargem))
                    .Replace("{{percReceitas}}", percReceitas.ToString("F1"))
                    .Replace("{{percDespesas}}", percDespesas.ToString("F1"))
                    .Replace("{{percMargem}}", percMargem.ToString("F1"))
                    .Replace("{{linhas}}", linhas)
                    .Replace("{{linhasReceitas}}", linhasReceitas)
                    .Replace("{{linhasDespesas}}", linhasDespesas)
                    .Replace("{{rebanhoTabelas}}", "") // TODO: Implementar se necessário
                    .Replace("{{linhasTotalizadorRebanho}}", "");

                // Remover a tag <link> original do CSS
                resultado = Regex.Replace(resultado, @"\s*<link\s+rel=""stylesheet""\s+href=""styles\.css""\s*/>", "");

                return resultado;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao preencher template: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gera linhas do relatório mensal (resumo por mês)
        /// </summary>
        private string GerarLinhasRelatoriMensalAsync(List<NotaFiscal> receitas, List<NotaFiscal> despesas)
        {
            var linhas = new System.Text.StringBuilder();
            var meses = Enumerable.Range(1, 12).ToList();

            foreach (var mes in meses)
            {
                var receitasMes = receitas.Where(n => n.DataEmissao.Month == mes).Sum(n => n.ValorNotaFiscal);
                var despesasMes = despesas.Where(n => n.DataEmissao.Month == mes).Sum(n => n.ValorNotaFiscal);

                var nomeMes = System.Globalization.CultureInfo.GetCultureInfo("pt-BR")
                    .DateTimeFormat.GetMonthName(mes);

                linhas.AppendLine("<tr>");
                linhas.AppendLine($"<td class=\"month\">{nomeMes}</td>");
                linhas.AppendLine($"<td>{FormatarMoeda(receitasMes)}</td>");
                linhas.AppendLine($"<td>{FormatarMoeda(despesasMes)}</td>");
                linhas.AppendLine("</tr>");
            }

            return linhas.ToString();
        }

        /// <summary>
        /// Gera linhas detalhadas de um conjunto de notas fiscais
        /// </summary>
        private string GerarLinhasDetalhadas(List<NotaFiscal> notas)
        {
            var linhas = new System.Text.StringBuilder();

            foreach (var nota in notas)
            {
                linhas.AppendLine("<tr>");
                linhas.AppendLine($"<td>{nota.DataEmissao:dd/MM/yyyy}</td>");
                linhas.AppendLine($"<td>{nota.NumeroDaNota}</td>");
                linhas.AppendLine($"<td>{nota.Origem}</td>");
                linhas.AppendLine($"<td>{nota.Descricao}</td>");
                linhas.AppendLine($"<td>{FormatarMoeda(nota.ValorNotaFiscal)}</td>");
                linhas.AppendLine("</tr>");
            }

            return linhas.ToString();
        }

        /// <summary>
        /// Formata um valor decimal como moeda brasileira
        /// </summary>
        private string FormatarMoeda(decimal valor)
        {
            return valor.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        }
    }
}
