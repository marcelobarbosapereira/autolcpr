using AutoLCPR.Application.Relatorios.Common;
using AutoLCPR.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoLCPR.Application.Relatorios.Documents;

internal sealed class RelatorioFinanceiroDocument : IDocument
{
    private readonly RelatorioFinanceiroDto _modelo;
    private readonly IReadOnlyList<Lancamento> _lancamentos;

    public RelatorioFinanceiroDocument(RelatorioFinanceiroDto modelo, IReadOnlyList<Lancamento> lancamentos)
    {
        _modelo = modelo;
        _lancamentos = lancamentos;
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
        var tituloPagina = _modelo.Tipo == TipoLancamento.Receita ? "Relatório de Receitas" : "Relatório de Despesas";
        var tituloSecao = _modelo.Tipo == TipoLancamento.Receita ? "RELATÓRIO DE RECEITAS" : "RELATÓRIO DE DESPESAS";

        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().AlignCenter().Text(tituloPagina).SemiBold().FontSize(16);
            column.Item().Text($"Data: {_modelo.DataGeracao:dd/MM/yy}").FontSize(9);
            column.Item().Text($"Hora: {_modelo.DataGeracao:HH:mm}").FontSize(9);
            column.Item().AlignCenter().Text(RelatorioPdfPadrao.SanitizarTexto(_modelo.NomeProdutor)).SemiBold().FontSize(14);
            column.Item().AlignCenter().Text(tituloSecao).SemiBold().FontSize(13);
            column.Item().AlignCenter().Text($"Período de {_modelo.DataInicial:dd/MM/yy} a {_modelo.DataFinal:dd/MM/yy}");
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#D1D5DB");
        });
    }

    private void ComporConteudo(IContainer container)
    {
        var colunaParceiro = _modelo.Tipo == TipoLancamento.Receita ? "Cliente" : "Fornecedor";
        var colunaDescricao = _modelo.Tipo == TipoLancamento.Receita ? "Descrição das Receitas" : "Descrição das Despesas";
        var labelTotal = _modelo.Tipo == TipoLancamento.Receita ? "Total das Receitas" : "Total das Despesas";

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

                if (_lancamentos.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignCenter().Text("Nenhum lançamento encontrado para o período informado.");
                    return;
                }

                foreach (var item in _lancamentos)
                {
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(item.Data.ToString("dd/MM/yyyy"));
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(RelatorioPdfPadrao.SanitizarTexto(item.ClienteFornecedor));
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).Text(RelatorioPdfPadrao.SanitizarTexto(item.Descricao));
                    table.Cell().Element(RelatorioPdfPadrao.EstiloCelulaTabela).AlignRight().Text(RelatorioPdfPadrao.FormatarMoeda(item.Valor));
                }
            });

            column.Item().AlignRight().Text(text =>
            {
                text.Span($"{labelTotal}: ").SemiBold();
                text.Span(RelatorioPdfPadrao.FormatarMoeda(_modelo.Total)).SemiBold();
            });
        });
    }
}