using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoLCPR.UI.WPF.Services;

public static class ReportExportService
{
    public static void ExportarPdf(string filePath, string titulo, string[] cabecalhos, IEnumerable<string[]> linhas)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var dadosLinhas = linhas.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(item => item.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text(titulo).Bold().FontSize(16);
                    column.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < cabecalhos.Length; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var cabecalho in cabecalhos)
                            {
                                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text(cabecalho).Bold();
                            }
                        });

                        foreach (var linha in dadosLinhas)
                        {
                            foreach (var campo in linha)
                            {
                                table.Cell().Element(CellStyle).Text(campo);
                            }
                        }
                    });
                });
            });
        }).GeneratePdf(filePath);

        static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(4)
                .PaddingHorizontal(6);
        }
    }

    public static void ExportarExcel(string filePath, string titulo, string[] cabecalhos, IEnumerable<string[]> linhas)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Relatorio");

        worksheet.Cell(1, 1).Value = titulo;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Value = $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";

        for (var i = 0; i < cabecalhos.Length; i++)
        {
            worksheet.Cell(4, i + 1).Value = cabecalhos[i];
            worksheet.Cell(4, i + 1).Style.Font.Bold = true;
        }

        var linhaAtual = 5;
        foreach (var linha in linhas)
        {
            for (var coluna = 0; coluna < linha.Length; coluna++)
            {
                worksheet.Cell(linhaAtual, coluna + 1).Value = linha[coluna];
            }

            linhaAtual++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
