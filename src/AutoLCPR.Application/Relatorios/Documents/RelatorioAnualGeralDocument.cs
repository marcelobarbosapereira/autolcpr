using System.Globalization;
using AutoLCPR.Application.Relatorios.Common;
using AutoLCPR.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoLCPR.Application.Relatorios.Documents;

internal sealed class RelatorioAnualGeralDocument : IDocument
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly RelatorioAnualDto _modelo;
    private readonly IReadOnlyList<Lancamento> _receitas;
    private readonly IReadOnlyList<Lancamento> _despesas;
    private readonly byte[]? _imagemCabecalho;

    public RelatorioAnualGeralDocument(
        RelatorioAnualDto modelo,
        IReadOnlyList<Lancamento> receitas,
        IReadOnlyList<Lancamento> despesas,
        byte[]? imagemCabecalho)
    {
        _modelo = modelo;
        _receitas = receitas;
        _despesas = despesas;
        _imagemCabecalho = imagemCabecalho is { Length: > 0 } ? imagemCabecalho : null;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        ComporFolhaRosto(container, "LIVRO CAIXA");
        ComporPaginaLivroCaixaResumo(container);
        ComporPaginaLivroCaixaConsolidado(container);

        ComporFolhaRosto(container, "RELATORIO DE RECEITAS");
        ComporTabelaNotasFiscais(container, "NOTAS FISCAIS - RECEITAS", _receitas);

        ComporFolhaRosto(container, "RELATORIO DE DESPESAS");
        ComporTabelaNotasFiscais(container, "NOTAS FISCAIS - DESPESAS", _despesas);

        ComporFolhaRosto(container, "RELATORIO DE REBANHOS");
        ComporPaginasRebanhos(container);
    }

    private void ComporFolhaRosto(IDocumentContainer container, string tituloSecao)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(12));

            page.Content().AlignMiddle().Column(column =>
            {
                column.Spacing(14);
                column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(20);
                column.Item().AlignCenter().Text(tituloSecao).SemiBold().FontSize(32);
                column.Item().AlignCenter().Text($"Ano Base: {_modelo.AnoBase}").FontSize(16);
                column.Item().AlignCenter().Text($"Ano Declaracao: {_modelo.AnoExercicio}").FontSize(16);
            });
        });
    }

    private void ComporPaginaLivroCaixaResumo(IDocumentContainer container)
    {
        var linhas = _modelo.ResumoMensal.OrderBy(x => x.Mes).ToList();

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginLeft(38);
            page.MarginRight(38);
            page.MarginTop(30);
            page.MarginBottom(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(18));

            page.Header().Element(container => ComporCabecalhoComImagem(container, col =>
            {
                col.Spacing(0);
                col.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).Bold().FontSize(18);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().PaddingTop(6).AlignCenter()
                    .Text($"DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE {_modelo.AnoBase}")
                    .FontSize(14);
            }));

            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(206);
                        cols.RelativeColumn(154);
                        cols.RelativeColumn(155);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(c => CabecalhoTabela(c, isFirst: true)).Text(string.Empty);
                        h.Cell().Element(c => CabecalhoTabela(c)).AlignCenter().Text("RECEITA");
                        h.Cell().Element(c => CabecalhoTabela(c, isLast: true)).AlignCenter().Text("DESPESAS");
                    });

                    foreach (var linha in linhas)
                    {
                        table.Cell().Element(c => CelulaTabela(c, isFirst: true)).Text(RelatorioPdfPadrao.SanitizarTexto(linha.NomeMes).ToUpperInvariant());
                        table.Cell().Element(c => CelulaTabela(c)).AlignRight().Text(linha.Receita == 0m ? "0,00" : FormataNumero(linha.Receita));
                        table.Cell().Element(c => CelulaTabela(c, isLast: true)).AlignRight().Text(FormataNumero(linha.Despesa));
                    }

                    table.Cell().Element(c => CelulaTotalBg(c, isFirst: true)).Text("TOTAL");
                    table.Cell().Element(c => CelulaTotalBg(c)).AlignRight().Text(FormataNumero(_modelo.TotalReceitas));
                    table.Cell().Element(c => CelulaTotalBg(c, isLast: true)).AlignRight().Text(FormataNumero(_modelo.TotalDespesas));
                });
            });
        });
    }

    private void ComporPaginaLivroCaixaConsolidado(IDocumentContainer container)
    {
        var totalReceitas = _modelo.TotalReceitas;
        var totalDespesas = _modelo.TotalDespesas;
        var margem = _modelo.MargemAnual;

        const float alturaMax = 314.6f;
        const decimal alturaMaxDecimal = 314.6m;
        var maxValor = Math.Max(totalReceitas, Math.Max(totalDespesas, Math.Max(1m, margem)));

        var alturaReceita = (float)(alturaMaxDecimal * (totalReceitas / maxValor));
        var alturaDespesa = (float)(alturaMaxDecimal * (totalDespesas / maxValor));
        var alturaMargem = (float)(alturaMaxDecimal * (margem / maxValor));

        var corReceita = "#00B050";
        var corDespesa = "#FF0000";
        var corMargem = "#00B0F0";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginLeft(38);
            page.MarginRight(38);
            page.MarginTop(30);
            page.MarginBottom(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(18));

            page.Header().Element(container => ComporCabecalhoComImagem(container, col =>
            {
                col.Spacing(0);
                col.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).Bold().FontSize(18);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().PaddingTop(6).AlignCenter()
                    .Text($"DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE {_modelo.AnoBase}")
                    .FontSize(14);
            }));

            page.Content().PaddingTop(14).Element(root =>
            {
                root.Height(700).Layers(layers =>
                {
                    layers.PrimaryLayer().Column(col =>
                    {
                        col.Item().AlignCenter().Text("DESPESAS X RECEITAS").Bold().FontSize(18);
                    });

                    layers.Layer().TranslateX(35).TranslateY(36).Width(430).Height(430).Row(row =>
                    {
                        row.Spacing(62);

                        row.ConstantItem(102).Column(c =>
                        {
                            c.Item().Height(alturaMax - alturaReceita);
                            c.Item().Height(alturaReceita).Background(corReceita);
                            c.Item().PaddingTop(4).AlignCenter().Text("RECEITA").Bold().FontSize(10);
                            c.Item().AlignCenter().Text("R$").FontSize(9);
                            c.Item().AlignCenter().Text(FormataNumero(totalReceitas)).FontSize(8);
                        });

                        row.ConstantItem(102).Column(c =>
                        {
                            c.Item().Height(alturaMax - alturaDespesa);
                            c.Item().Height(alturaDespesa).Background(corDespesa);
                            c.Item().PaddingTop(4).AlignCenter().Text("DESPESAS").Bold().FontSize(10);
                            c.Item().AlignCenter().Text("R$").FontSize(9);
                            c.Item().AlignCenter().Text(FormataNumero(totalDespesas)).FontSize(8);
                        });

                        row.ConstantItem(102).Column(c =>
                        {
                            c.Item().Height(alturaMax - alturaMargem);
                            c.Item().Height(alturaMargem).Background(corMargem);
                            c.Item().PaddingTop(4).AlignCenter().Text("MARGEM").Bold().FontSize(10);
                            c.Item().AlignCenter().Text("R$").FontSize(9);
                            c.Item().AlignCenter().Text(FormataNumero(margem)).FontSize(8);
                        });
                    });

                    layers.Layer().TranslateY(560).Height(66).Row(row =>
                    {
                        row.Spacing(8);

                        row.RelativeItem().Height(66).Border(1).BorderColor(Colors.Black).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingVertical(4).Text("RECEITAS").Bold().FontSize(16);
                            c.Item().LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().PaddingVertical(4).Text($"R$               {FormataNumero(totalReceitas)}").FontSize(11);
                        });

                        row.RelativeItem().Height(66).Border(1).BorderColor(Colors.Black).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingVertical(4).Text("DESPESAS").Bold().FontSize(16);
                            c.Item().LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().PaddingVertical(4).Text($"R$               {FormataNumero(totalDespesas)}").FontSize(11);
                        });

                        row.RelativeItem().Height(66).Border(1).BorderColor(Colors.Black).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingVertical(4).Text("MARGEM").Bold().FontSize(16);
                            c.Item().LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().PaddingVertical(4).Text($"R$                 {FormataNumero(margem)}").FontSize(11);
                        });
                    });
                });
            });
        });
    }

    private void ComporTabelaNotasFiscais(IDocumentContainer container, string titulo, IReadOnlyList<Lancamento> lancamentos)
    {
        var linhas = lancamentos
            .OrderBy(x => x.Data)
            .Select(x => new NotaFiscalLinha(
                x.Data,
                RelatorioPdfPadrao.SanitizarTexto(x.ClienteFornecedor),
                RelatorioPdfPadrao.SanitizarTexto(x.Descricao),
                x.Valor))
            .ToList();

        var total = linhas.Sum(x => x.Valor);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(10));

            page.Header().Element(container => ComporCabecalhoComImagem(container, col =>
            {
                col.Spacing(3);
                col.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(13);
                col.Item().AlignCenter().Text($"{titulo} - ANO BASE {_modelo.AnoBase}").SemiBold().FontSize(12);
                col.Item().LineHorizontal(1).LineColor(Colors.Black);
            }));

            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(105);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(120);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TabelaNotaHeader).Text("Data de Emissao");
                    header.Cell().Element(TabelaNotaHeader).Text("Nome da Origem");
                    header.Cell().Element(TabelaNotaHeader).Text("Descricao");
                    header.Cell().Element(TabelaNotaHeader).AlignRight().Text("Valor da Nota Fiscal");
                });

                if (linhas.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Element(TabelaNotaBody).AlignCenter().Text("Nenhuma nota fiscal relacionada nesta secao.");
                }
                else
                {
                    foreach (var linha in linhas)
                    {
                        table.Cell().Element(TabelaNotaBody).Text(linha.DataEmissao.ToString("dd/MM/yyyy"));
                        table.Cell().Element(TabelaNotaBody).Text(linha.NomeOrigem);
                        table.Cell().Element(TabelaNotaBody).Text(linha.Descricao);
                        table.Cell().Element(TabelaNotaBody).AlignRight().Text($"R$ {FormataNumero(linha.Valor)}");
                    }
                }

                table.Cell().ColumnSpan(3).Element(TabelaNotaTotal).AlignRight().Text("Total das notas fiscais relacionadas");
                table.Cell().Element(TabelaNotaTotal).AlignRight().Text($"R$ {FormataNumero(total)}");
            });
        });
    }

    private void ComporPaginasRebanhos(IDocumentContainer container)
    {
        var linhas = _modelo.Propriedades
            .Select(x => new ResumoRebanhoLinha(
                RelatorioPdfPadrao.SanitizarTexto(x.NomePropriedade),
                RelatorioPdfPadrao.SanitizarTexto(x.InscricaoPropriedade),
                x.TotalNascimentos,
                x.TotalObitos,
                x.TotalCompras,
                x.TotalVendas))
            .ToList();

        var totalNascimentos = linhas.Sum(x => x.Nascimentos);
        var totalMortesConsumo = linhas.Sum(x => x.MortesConsumo);
        var totalEntradas = linhas.Sum(x => x.Entradas);
        var totalSaidas = linhas.Sum(x => x.Saidas);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(10));

            page.Header().Element(container => ComporCabecalhoComImagem(container, col =>
            {
                col.Spacing(3);
                col.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(13);
                col.Item().AlignCenter().Text($"RELATORIO DE REBANHOS - ANO BASE {_modelo.AnoBase}").SemiBold().FontSize(12);
                col.Item().LineHorizontal(1).LineColor(Colors.Black);
            }));

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(8);

                foreach (var linha in linhas)
                {
                    col.Item().ShowEntire().Border(1).BorderColor(Colors.Black).Padding(6).Column(bloco =>
                    {
                        bloco.Spacing(4);
                        bloco.Item().Text($"Nome do Rebanho: {linha.Nome}").SemiBold();
                        bloco.Item().Text($"Inscricao do Rebanho: {linha.Inscricao}").SemiBold();

                        bloco.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(TabelaRebanhoHeader).Text("Movimentacao");
                                header.Cell().Element(TabelaRebanhoHeader).AlignRight().Text("Quantidade");
                            });

                            table.Cell().Element(TabelaRebanhoBody).Text("Nascimentos");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(linha.Nascimentos.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Mortes/Consumo");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(linha.MortesConsumo.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Entradas");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(linha.Entradas.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Saidas");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(linha.Saidas.ToString(PtBr));
                        });
                    });
                }

                col.Item().ShowEntire().PaddingTop(8).Border(1).BorderColor(Colors.Black).Padding(6).Column(resumo =>
                {
                    resumo.Spacing(4);
                    resumo.Item().Text("Resumo Geral dos Rebanhos").SemiBold().FontSize(11);

                    resumo.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(TabelaRebanhoHeader).Text("Movimentacao");
                            header.Cell().Element(TabelaRebanhoHeader).AlignRight().Text("Total");
                        });

                        table.Cell().Element(TabelaRebanhoBody).Text("Nascimentos");
                        table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(totalNascimentos.ToString(PtBr));

                        table.Cell().Element(TabelaRebanhoBody).Text("Mortes/Consumo");
                        table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(totalMortesConsumo.ToString(PtBr));

                        table.Cell().Element(TabelaRebanhoBody).Text("Entradas");
                        table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(totalEntradas.ToString(PtBr));

                        table.Cell().Element(TabelaRebanhoBody).Text("Saidas");
                        table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(totalSaidas.ToString(PtBr));
                    });
                });
            });
        });
    }

    private static string FormataNumero(decimal valor)
        => string.Format(PtBr, "{0:N2}", valor);

    private void ComporCabecalhoComImagem(IContainer container, Action<ColumnDescriptor> conteudoCabecalho)
    {
        container.Layers(layers =>
        {
            layers.PrimaryLayer().Column(conteudoCabecalho);

            if (_imagemCabecalho is { Length: > 0 })
            {
                layers.Layer()
                    .AlignLeft()
                    .TranslateY(2)
                    .Height(26)
                    .Image(_imagemCabecalho)
                    .FitHeight();
            }
        });
    }

    private static IContainer CabecalhoTabela(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .DefaultTextStyle(t => t.Bold().FontSize(18));
    }

    private static IContainer CelulaTabela(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(t => t.FontSize(18));
    }

    private static IContainer CelulaTotalBg(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .Background("#D9D9D9")
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(t => t.Bold().FontSize(18));
    }

    private static IContainer TabelaNotaHeader(IContainer c)
        => c.Border(1)
            .BorderColor(Colors.Black)
            .Background("#EDEDED")
            .Padding(5)
            .DefaultTextStyle(t => t.SemiBold());

    private static IContainer TabelaNotaBody(IContainer c)
        => c.Border(1)
            .BorderColor(Colors.Black)
            .Padding(5);

    private static IContainer TabelaNotaTotal(IContainer c)
        => c.Border(1)
            .BorderColor(Colors.Black)
            .Background("#D9D9D9")
            .Padding(5)
            .DefaultTextStyle(t => t.SemiBold());

    private static IContainer TabelaRebanhoHeader(IContainer c)
        => c.Border(1)
            .BorderColor(Colors.Black)
            .Background("#EDEDED")
            .Padding(6)
            .DefaultTextStyle(t => t.SemiBold());

    private static IContainer TabelaRebanhoBody(IContainer c)
        => c.Border(1)
            .BorderColor(Colors.Black)
            .Padding(6);

    private sealed record NotaFiscalLinha(DateTime DataEmissao, string NomeOrigem, string Descricao, decimal Valor);
    private sealed record ResumoRebanhoLinha(string Nome, string Inscricao, int Nascimentos, int MortesConsumo, int Entradas, int Saidas);
}
