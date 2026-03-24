using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoLCPR.Application.Relatorios.Common;

internal static class RelatorioPdfPadrao
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static void ConfigurarPagina(PageDescriptor page, bool mostrarRodape = true)
    {
        page.Size(PageSizes.A4);
        page.Margin(28);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(text => text.FontFamily(Fonts.Arial).FontSize(10).FontColor("#1F2937"));

        if (mostrarRodape)
        {
            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                });
        }
    }

    public static IContainer EstiloCabecalhoTabela(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#B4B4B4")
            .Background("#EBEBEB")
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }

    public static IContainer EstiloCelulaTabela(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#C8C8C8")
            .PaddingVertical(5)
            .PaddingHorizontal(8);
    }

    public static IContainer EstiloCelulaTabelaDestaque(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#A0A0A0")
            .Background("#F5F5F5")
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }

    public static string FormatarMoeda(decimal valor)
    {
        return string.Format(PtBr, "{0:C}", valor);
    }

    public static string SanitizarTexto(string? valor)
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