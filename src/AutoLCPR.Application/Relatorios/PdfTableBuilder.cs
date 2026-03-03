using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace AutoLCPR.Application.Relatorios;

internal static class PdfTableBuilder
{
    public static Table CriarTabela(float[] larguras, IReadOnlyList<string> cabecalhos, PdfFont fonteCabecalho)
    {
        var tabela = new Table(UnitValue.CreatePercentArray(larguras)).UseAllAvailableWidth();

        foreach (var cabecalho in cabecalhos)
        {
            tabela.AddHeaderCell(new Cell()
                .Add(new Paragraph(cabecalho).SetFont(fonteCabecalho).SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(235, 235, 235))
                .SetBorder(new SolidBorder(new DeviceRgb(180, 180, 180), 0.8f))
                .SetTextAlignment(TextAlignment.CENTER));
        }

        return tabela;
    }

    public static void AdicionarLinha(Table tabela, IReadOnlyList<string> valores, PdfFont fonteConteudo, TextAlignment[]? alinhamentos = null)
    {
        for (var indice = 0; indice < valores.Count; indice++)
        {
            var alinhamento = alinhamentos != null && indice < alinhamentos.Length
                ? alinhamentos[indice]
                : TextAlignment.LEFT;

            tabela.AddCell(new Cell()
                .Add(new Paragraph(SanitizarTexto(valores[indice])).SetFont(fonteConteudo).SetFontSize(9))
                .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
                .SetTextAlignment(alinhamento));
        }
    }

    private static string SanitizarTexto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var caracteres = valor
            .Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
            .ToArray();

        return new string(caracteres).Trim();
    }
}
