using AutoLCPR.Application.DTOs;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoLCPR.Application.Services;

public sealed class FolhaPagamentoPdfParserService : IFolhaPagamentoPdfParserService
{
    // Estrutura real do PDF Folha (linha de header + linha de empregado):
    //   "Folha Mensal Filial: 1 Competencia: 01/2025 Vencimento: 31/01/2025 2.762,38 ..."
    //   "4-Ademiro Bernardes da Silva 2.762,38 0,00 2.762,38"
    // Estrutura real do PDF Encargos:
    //   "INSS Mensal 1 - 12/2024 20/01/2025 313,93 0,00 ..."
    // Usando caracteres Unicode literais e RegexOptions.Multiline para ^ ancorar por linha.

    private static readonly Regex RxFolhaHeader = new(
        "^(Folha Mensal|F\u00e9rias|13\u00ba\\s*Adiantamento|13\u00ba\\s*Integral)" +
        @"\s+Filial:\s*\d+\s+Compet[e\u00ea]ncia:\s*(\d{2}/\d{4})\s+Vencimento:\s*(\d{2}/\d{2}/\d{4})\s+([\d.]+,\d{2})",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex RxEmpregado = new(
        @"^\d+\s*-\s*(.+?)\s+([\d.]+,\d{2})\s+[\d.,]+\s+[\d.,]+\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex RxEncargo = new(
        "^(INSS\\s+13\u00ba|INSS\\s+Mensal|FGTS\\s+13\u00ba|FGTS)" +
        @"\s+\d+\s+-\s+(\d{2}/\d{4})\s+(\d{2}/\d{2}/\d{4})\s+([\d.]+,\d{2})",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public Task<FolhaPagamentoPdfDTO> ParseAsync(string caminhoPdf, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(caminhoPdf))
            throw new ArgumentException("O caminho do PDF nao foi informado.", nameof(caminhoPdf));
        if (!File.Exists(caminhoPdf))
            throw new FileNotFoundException("Arquivo nao encontrado.", caminhoPdf);
        if (!string.Equals(Path.GetExtension(caminhoPdf), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Formato invalido. O arquivo deve ser um PDF.");
        try
        {
            return Task.FromResult(ExtrairDadosPdf(caminhoPdf));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new FolhaPagamentoPdfDTO
            {
                ArquivoOrigem = caminhoPdf,
                ParserImplementado = false,
                Observacao = $"Erro ao processar PDF: {ex.Message}"
            });
        }
    }

    private FolhaPagamentoPdfDTO ExtrairDadosPdf(string caminhoPdf)
    {
        var dto = new FolhaPagamentoPdfDTO { ArquivoOrigem = caminhoPdf };

        var sb = new System.Text.StringBuilder();
        using (var pdfReader = new PdfReader(caminhoPdf))
        using (var pdfDocument = new PdfDocument(pdfReader))
        {
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                // LocationTextExtractionStrategy ordena chunks por posicao (X,Y),
                // produzindo texto em ordem de leitura real (linha a linha).
                var strategy = new LocationTextExtractionStrategy();
                sb.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy));
            }
        }

        var texto = sb.ToString();
        SalvarDebug(caminhoPdf, texto);

        ExtrairCabecalho(texto, dto);

        var ehEncargos = texto.IndexOf("PAGAMENTO DE ENCARGOS", StringComparison.OrdinalIgnoreCase) >= 0;
        if (ehEncargos)
            ParsearEncargos(texto, dto);
        else
            ParsearFolha(texto, dto);

        dto.ParserImplementado = true;
        dto.Observacao = dto.Itens.Count > 0
            ? $"{dto.Itens.Count} item(ns) extraido(s) com sucesso."
            : $"Nenhum item encontrado. Diagnostico: {DebugPath(caminhoPdf)}";

        return dto;
    }

    private static void ParsearFolha(string texto, FolhaPagamentoPdfDTO dto)
    {
        var linhas = texto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var naoVazias = linhas
            .Select((l, i) => (idx: i, txt: l.Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x.txt))
            .ToList();

        for (int k = 0; k < naoVazias.Count; k++)
        {
            var mH = RxFolhaHeader.Match(naoVazias[k].txt);
            if (!mH.Success) continue;

            var tipo = NormalizarTipo(mH.Groups[1].Value);
            var comp = mH.Groups[2].Value;
            var venc = ParseData(mH.Groups[3].Value);
            var valorH = ParseDecimal(mH.Groups[4].Value);

            for (int j = k + 1; j < Math.Min(k + 4, naoVazias.Count); j++)
            {
                var mE = RxEmpregado.Match(naoVazias[j].txt);
                if (!mE.Success) continue;
                var valor = ParseDecimal(mE.Groups[2].Value);
                dto.Itens.Add(new FolhaPagamentoItemDTO
                {
                    TipoCalculo = tipo,
                    Empregado = mE.Groups[1].Value.Trim(),
                    Competencia = comp,
                    Vencimento = venc,
                    Valor = valor > 0 ? valor : valorH,
                    ArquivoOrigem = dto.ArquivoOrigem
                });
                break;
            }
        }
    }

    private static void ParsearEncargos(string texto, FolhaPagamentoPdfDTO dto)
    {
        foreach (Match m in RxEncargo.Matches(texto))
        {
            var tipo = NormalizarTipo(m.Groups[1].Value);
            dto.Itens.Add(new FolhaPagamentoItemDTO
            {
                TipoCalculo = tipo,
                Empregado = ClienteFornecedorPorEncargo(tipo),
                Competencia = m.Groups[2].Value,
                Vencimento = ParseData(m.Groups[3].Value),
                Valor = ParseDecimal(m.Groups[4].Value),
                ArquivoOrigem = dto.ArquivoOrigem
            });
        }
    }

    private static void ExtrairCabecalho(string texto, FolhaPagamentoPdfDTO dto)
    {
        var linhas = texto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var linha in linhas.Take(20))
        {
            var l = linha.Trim();
            if (dto.Competencia == null && Regex.IsMatch(l, @"\d{2}/\d{4}\s+a\s+\d{2}/\d{4}"))
            { dto.Competencia = l; continue; }
            if (dto.Empresa == null && l.Length > 5 && !l.Contains('/')
                && !Regex.IsMatch(l, @"^\d+$")
                && !l.StartsWith("PAGAMENTO", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("CEI", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Empresa", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Tipo", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("P\u00e1gina", StringComparison.OrdinalIgnoreCase))
            { dto.Empresa = l; }
        }
    }

    private static string NormalizarTipo(string tipo)
    {
        var t = Regex.Replace(tipo.Trim(), @"\s+", " ");
        return Regex.Replace(t, "13[o\u00ba\u00b0]", "13\u00ba");
    }

    private static string ClienteFornecedorPorEncargo(string tipo)
    {
        if (tipo.StartsWith("INSS", StringComparison.OrdinalIgnoreCase)) return "Receita Federal";
        if (tipo.StartsWith("FGTS", StringComparison.OrdinalIgnoreCase)) return "Caixa Econ\u00f4mica Federal";
        return tipo;
    }

    private static DateTime? ParseData(string v)
    {
        return DateTime.TryParseExact(v.Trim(), "dd/MM/yyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
    }

    private static decimal ParseDecimal(string v)
    {
        var n = v.Trim().Replace(".", "").Replace(",", ".");
        return decimal.TryParse(n, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static string DebugPath(string p) =>
        Path.Combine(Path.GetTempPath(), $"autolcpr_folha_debug_{Path.GetFileNameWithoutExtension(p)}.txt");

    private static void SalvarDebug(string caminhoPdf, string texto)
    {
        try { File.WriteAllText(DebugPath(caminhoPdf), texto, System.Text.Encoding.UTF8); }
        catch { }
    }
}
