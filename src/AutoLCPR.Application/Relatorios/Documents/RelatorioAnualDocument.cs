using AutoLCPR.Application.Relatorios.Common;
using AutoLCPR.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoLCPR.Application.Relatorios.Documents;

internal sealed class RelatorioAnualDocument : IDocument
{
    private readonly RelatorioAnualDto _modelo;
    private readonly IReadOnlyList<Lancamento> _receitas;
    private readonly IReadOnlyList<Lancamento> _despesas;

    public RelatorioAnualDocument(RelatorioAnualDto modelo, IReadOnlyList<Lancamento> receitas, IReadOnlyList<Lancamento> despesas)
    {
        _modelo = modelo;
        _receitas = receitas;
        _despesas = despesas;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(ComporCapa);
        container.Page(ComporResumoAnual);
        container.Page(page => ComporPaginaLancamentos(page, "RECEITAS", "RELATÓRIO DE RECEITAS", "Cliente", "Descrição das Receitas", "Total das Receitas", _receitas, _modelo.TotalReceitas));
        container.Page(page => ComporPaginaLancamentos(page, "DESPESAS", "RELATÓRIO DE DESPESAS", "Fornecedor", "Descrição das Despesas", "Total das Despesas", _despesas, _modelo.TotalDespesas));
        container.Page(ComporPaginaRebanho);
    }

    private void ComporCapa(PageDescriptor page)
    {
        RelatorioPdfPadrao.ConfigurarPagina(page, mostrarRodape: false);

        page.Content().AlignMiddle().Column(column =>
        {
            column.Spacing(14);
            column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(18);
            column.Item().AlignCenter().Text("LIVRO CAIXA").SemiBold().FontSize(30);
            column.Item().AlignCenter().Text($"EX: {_modelo.AnoExercicio}").FontSize(16);
            column.Item().AlignCenter().Text($"ANO BASE: {_modelo.AnoBase}").FontSize(14);
        });
    }

    private void ComporResumoAnual(PageDescriptor page)
    {
        RelatorioPdfPadrao.ConfigurarPagina(page);
        page.Header().Element(container => ComporCabecalhoLivroCaixa(container, "DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE", incluirDataHora: false));
        page.Content().Element(ComporResumoConteudo);
    }

    private void ComporResumoConteudo(IContainer container)
    {
        container.PaddingTop(14).Column(column =>
        {
            column.Spacing(14);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Mês").SemiBold();
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Receita").SemiBold();
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Despesa").SemiBold();
                });

                foreach (var item in _modelo.ResumoMensal)
                {
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(RelatorioPdfPadrao.SanitizarTexto(item.NomeMes));
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(item.Receita));
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(item.Despesa));
                }

                table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabelaDestaque).Text("TOTAL").SemiBold();
                table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabelaDestaque).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(_modelo.TotalReceitas)).SemiBold();
                table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabelaDestaque).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(_modelo.TotalDespesas)).SemiBold();
            });

            column.Item().Text($"RECEITA: {RelatorioPdfPadrao.FormatarMoeda(_modelo.TotalReceitas)}");
            column.Item().Text($"DESPESAS: {RelatorioPdfPadrao.FormatarMoeda(_modelo.TotalDespesas)}");
            column.Item().Text($"MARGEM: {RelatorioPdfPadrao.FormatarMoeda(_modelo.MargemAnual)}").SemiBold();

            try
            {
                var grafico = RelatorioGraficoFactory.CriarGraficoComparativo(_modelo.ResumoMensal);
                column.Item().PaddingTop(8).Image(grafico).FitWidth();
            }
            catch
            {
                column.Item().Text("Gráfico comparativo indisponível para este relatório.").FontSize(9);
            }
        });
    }

    private void ComporPaginaLancamentos(
        PageDescriptor page,
        string tituloSecao,
        string tituloRelatorio,
        string colunaParceiro,
        string colunaDescricao,
        string labelTotal,
        IReadOnlyList<Lancamento> lancamentos,
        decimal total)
    {
        RelatorioPdfPadrao.ConfigurarPagina(page);
        page.Header().Element(container => ComporCabecalhoSecao(container, tituloSecao, tituloRelatorio));
        page.Content().Element(container => ComporConteudoLancamentos(container, colunaParceiro, colunaDescricao, labelTotal, lancamentos, total));
    }

    private void ComporConteudoLancamentos(
        IContainer container,
        string colunaParceiro,
        string colunaDescricao,
        string labelTotal,
        IReadOnlyList<Lancamento> lancamentos,
        decimal total)
    {
        container.PaddingTop(14).Column(column =>
        {
            column.Spacing(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(13);
                    columns.RelativeColumn(22);
                    columns.RelativeColumn(40);
                    columns.RelativeColumn(15);
                });

                table.Header(header =>
                {
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Data").SemiBold();
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text(colunaParceiro).SemiBold();
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text(colunaDescricao).SemiBold();
                    header.Cell().Element(RelatorioPdfPadrao.EstiloCabecalhoTabela).AlignCenter().Text("Valor").SemiBold();
                });

                if (lancamentos.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignCenter().Text("Nenhum lançamento encontrado para esta seção.");
                }
                else
                {
                    foreach (var item in lancamentos)
                    {
                        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(item.Data.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(RelatorioPdfPadrao.SanitizarTexto(item.ClienteFornecedor));
                        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(RelatorioPdfPadrao.SanitizarTexto(item.Descricao));
                        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(item.Valor));
                    }
                }
            });

            column.Item().AlignRight().Text(text =>
            {
                text.Span($"{labelTotal}: ").SemiBold();
                text.Span(RelatorioPdfPadrao.FormatarMoeda(total)).SemiBold();
            });
        });
    }

    private void ComporPaginaRebanho(PageDescriptor page)
    {
        RelatorioPdfPadrao.ConfigurarPagina(page);
        page.Header().Element(container => ComporCabecalhoSecao(container, "REBANHOS", "RELATÓRIO DE MOVIMENTAÇÃO DE REBANHO"));
        page.Content().Element(ComporConteudoRebanho);
    }

    private void ComporConteudoRebanho(IContainer container)
    {
        var totalNascimentos = _modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Nascimentos", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalCompras = _modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Compras", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalVendas = _modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Vendas", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalObitos = _modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Óbitos", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var saldoRebanhoAno = totalNascimentos + totalCompras - totalVendas - totalObitos;

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

                        AdicionarLinhaRebanho(table, "Nascimentos", propriedade.TotalNascimentos);
                        AdicionarLinhaRebanho(table, "Compras", propriedade.TotalCompras);
                        AdicionarLinhaRebanho(table, "Vendas", propriedade.TotalVendas);
                        AdicionarLinhaRebanho(table, "Óbitos", propriedade.TotalObitos);
                    });
                });
            }

            column.Item().PaddingTop(8).Column(resumo =>
            {
                resumo.Spacing(4);
                resumo.Item().Text($"Total de Nascimentos: {totalNascimentos}");
                resumo.Item().Text($"Total de Compras: {totalCompras}");
                resumo.Item().Text($"Total de Vendas: {totalVendas}");
                resumo.Item().Text($"Total de Óbitos: {totalObitos}");
                resumo.Item().Text($"Saldo do Rebanho no Ano: {saldoRebanhoAno}").SemiBold();
            });
        });
    }

    private void ComporCabecalhoLivroCaixa(IContainer container, string tituloRelatorio, bool incluirDataHora)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(14);
            column.Item().AlignCenter().Text("LIVRO CAIXA").SemiBold().FontSize(20);
            column.Item().AlignCenter().Text($"({_modelo.DataInicio:dd/MM/yyyy} a {_modelo.DataFim:dd/MM/yyyy})");
            column.Item().AlignCenter().Text($"EX: {_modelo.AnoExercicio}");
            column.Item().AlignCenter().Text($"ANO BASE: {_modelo.AnoBase}");

            if (incluirDataHora)
            {
                column.Item().Text($"Data: {_modelo.DataGeracao:dd/MM/yy}").FontSize(9);
                column.Item().Text($"Hora: {_modelo.DataGeracao:HH:mm}").FontSize(9);
            }

            column.Item().AlignCenter().Text(tituloRelatorio).SemiBold().FontSize(13);
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#D1D5DB");
        });
    }

    private void ComporCabecalhoSecao(IContainer container, string tituloSecao, string tituloRelatorio)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(14);
            column.Item().AlignCenter().Text(tituloSecao).SemiBold().FontSize(20);
            column.Item().AlignCenter().Text($"({_modelo.DataInicio:dd/MM/yyyy} a {_modelo.DataFim:dd/MM/yyyy})");
            column.Item().AlignCenter().Text($"EX: {_modelo.AnoExercicio}");
            column.Item().AlignCenter().Text($"ANO BASE: {_modelo.AnoBase}");
            column.Item().Text($"Data: {_modelo.DataGeracao:dd/MM/yy}").FontSize(9);
            column.Item().Text($"Hora: {_modelo.DataGeracao:HH:mm}").FontSize(9);
            column.Item().AlignCenter().Text(tituloRelatorio).SemiBold().FontSize(13);
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#D1D5DB");
        });
    }

    private static void AdicionarLinhaRebanho(TableDescriptor table, string label, int valor)
    {
        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(label);
        table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(valor.ToString());
    }
}