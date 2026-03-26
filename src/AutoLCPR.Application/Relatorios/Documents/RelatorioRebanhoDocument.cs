using AutoLCPR.Application.Relatorios.Common;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoLCPR.Application.Relatorios.Documents;

internal sealed class RelatorioRebanhoDocument : IDocument
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private readonly RelatorioRebanhoDto _modelo;

    public RelatorioRebanhoDocument(RelatorioRebanhoDto modelo)
    {
        _modelo = modelo;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            RelatorioPdfPadrao.ConfigurarPagina(page);

            page.Header().Element(ComporCabecalho);
            page.Content().Element(ComporConteudo);
        });
    }

    private void ComporCabecalho(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(14);
            column.Item().AlignCenter().Text("REBANHOS").SemiBold().FontSize(20);
            column.Item().AlignCenter().Text($"({_modelo.DataInicio:dd/MM/yyyy} a {_modelo.DataFim:dd/MM/yyyy})");
            column.Item().AlignCenter().Text($"EX: {_modelo.AnoExercicio}");
            column.Item().AlignCenter().Text($"ANO BASE: {_modelo.AnoBase}");
            column.Item().Text($"Data: {_modelo.DataGeracao:dd/MM/yy}").FontSize(9);
            column.Item().Text($"Hora: {_modelo.DataGeracao:HH:mm}").FontSize(9);
            column.Item().AlignCenter().Text("RELATÓRIO DE MOVIMENTAÇÃO DE REBANHO").SemiBold().FontSize(13);
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#D1D5DB");
        });
    }

    private void ComporConteudo(IContainer container)
    {
        container.PaddingTop(14).Column(column =>
        {
            column.Spacing(12);

            foreach (var propriedade in _modelo.Propriedades)
            {
                column.Item().Border(1).BorderColor("#D1D5DB").Padding(12).Column(bloco =>
                {
                    bloco.Spacing(8);
                    bloco.Item().Text(text =>
                    {
                        text.Span("Nome da propriedade: ").SemiBold();
                        text.Span(RelatorioPdfPadrao.SanitizarTexto(propriedade.NomePropriedade));
                    });
                    bloco.Item().Text($"Inscrição: {RelatorioPdfPadrao.SanitizarTexto(propriedade.InscricaoPropriedade)}");
                    bloco.Item().Text(text =>
                    {
                        text.Span("Saldo Inicial: ").SemiBold();
                        text.Span(propriedade.SaldoInicial.ToString("C", PtBr));
                    });
                    bloco.Item().Text(text =>
                    {
                        text.Span("Saldo Final: ").SemiBold();
                        text.Span(propriedade.SaldoFinal.ToString("C", PtBr));
                    });
                    bloco.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text(string.Empty).SemiBold();
                            header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Quantidade").SemiBold();
                        });

                        AdicionarLinha(table, "Nascimentos", propriedade.TotalNascimentos);
                        AdicionarLinha(table, "Compras", propriedade.TotalCompras);
                        AdicionarLinha(table, "Vendas", propriedade.TotalVendas);
                        AdicionarLinha(table, "Óbitos", propriedade.TotalObitos);
                    });
                });
            }

            column.Item().PaddingTop(8).Column(resumo =>
            {
                resumo.Spacing(4);
                resumo.Item().Text($"Total de Nascimentos: {_modelo.Resumo.TotalNascimentos}");
                resumo.Item().Text($"Total de Compras: {_modelo.Resumo.TotalCompras}");
                resumo.Item().Text($"Total de Vendas: {_modelo.Resumo.TotalVendas}");
                resumo.Item().Text($"Total de Óbitos: {_modelo.Resumo.TotalObitos}");
                resumo.Item().Text($"Saldo Inicial Total: {_modelo.Resumo.TotalSaldoInicial.ToString("C", PtBr)}").SemiBold();
                resumo.Item().Text($"Saldo Final Total: {_modelo.Resumo.TotalSaldoFinal.ToString("C", PtBr)}").SemiBold();
                resumo.Item().Text($"Saldo do Rebanho no Ano: {_modelo.Resumo.SaldoRebanhoAno}").SemiBold();
            });
        });
    }

    private static void AdicionarLinha(TableDescriptor table, string label, int valor)
    {
        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(label);
        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(valor.ToString(PtBr));
    }
}