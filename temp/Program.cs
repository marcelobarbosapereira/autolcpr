using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

public static class Program
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static void Main()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var nomeCliente = "Joao da Silva";
        var anoBase = 2024;
        var anoDeclaracao = 2025;

        var notasReceitas = new List<NotaFiscal>
        {
            new(new DateTime(2024, 1, 10), "Cooperativa Vale", "Venda de bovinos", 45200.50m),
            new(new DateTime(2024, 2, 15), "Frigorifico Central", "Venda de gado de corte", 38990.00m),
            new(new DateTime(2024, 3, 8), "Laticinios Serra", "Venda de leite", 12450.75m),
            new(new DateTime(2024, 3, 28), "Mercado Rural", "Venda de insumos excedentes", 6880.20m),
        };

        var notasDespesas = new List<NotaFiscal>
        {
            new(new DateTime(2024, 1, 5), "Agro Forte", "Compra de racao", 9800.00m),
            new(new DateTime(2024, 2, 18), "Vet Prime", "Medicamentos veterinarios", 3150.40m),
            new(new DateTime(2024, 4, 2), "Combustivel Campo", "Diesel para maquinario", 7420.32m),
            new(new DateTime(2024, 5, 11), "Eletrica Rural", "Manutencao de cerca eletrica", 2890.00m),
        };

        var rebanhos = new List<ResumoRebanho>
        {
            new("Rebanho Matriz", "RBH-001", Nascimentos: 42, MortesConsumo: 7, Entradas: 16, Saidas: 21),
            new("Rebanho Engorda", "RBH-002", Nascimentos: 18, MortesConsumo: 5, Entradas: 34, Saidas: 29),
            new("Rebanho Leiteiro", "RBH-003", Nascimentos: 27, MortesConsumo: 4, Entradas: 10, Saidas: 13),
        };

        var document = Document.Create(container =>
        {
            ComporFolhaRosto(container, nomeCliente, "LIVRO CAIXA", anoBase, anoDeclaracao);
            ComporPaginaLivroCaixaResumo(container, nomeCliente, anoBase, anoDeclaracao);
            ComporPaginaLivroCaixaConsolidado(container, nomeCliente, anoBase, anoDeclaracao);

            ComporFolhaRosto(container, nomeCliente, "RELATORIO DE RECEITAS", anoBase, anoDeclaracao);
            ComporTabelaNotasFiscais(container, nomeCliente, anoBase, "NOTAS FISCAIS - RECEITAS", notasReceitas);
            ComporFolhaRosto(container, nomeCliente, "RELATORIO DE DESPESAS", anoBase, anoDeclaracao);
            ComporTabelaNotasFiscais(container, nomeCliente, anoBase, "NOTAS FISCAIS - DESPESAS", notasDespesas);
            ComporFolhaRosto(container, nomeCliente, "RELATORIO DE REBANHOS", anoBase, anoDeclaracao);
            ComporPaginasRebanhos(container, nomeCliente, anoBase, rebanhos);
        });

        var outputPath = Path.Combine(AppContext.BaseDirectory, "preview-teste.pdf");
        document.GeneratePdf(outputPath);

        Console.WriteLine($"PDF gerado em: {outputPath}");
    }

    private static void ComporFolhaRosto(IDocumentContainer container, string nomeCliente, string titulo, int anoBase, int anoDeclaracao)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(12));

            page.Content().AlignMiddle().Column(column =>
            {
                column.Spacing(14);
                column.Item().AlignCenter().Text(nomeCliente).SemiBold().FontSize(20);
                column.Item().AlignCenter().Text(titulo).SemiBold().FontSize(32);
                column.Item().AlignCenter().Text($"Ano Base: {anoBase}").FontSize(16);
                column.Item().AlignCenter().Text($"Ano Declaracao: {anoDeclaracao}").FontSize(16);
            });
        });
    }

    // ─── PÁGINA 1: tabela mensal — idêntica ao PDF de exemplo ───────────────
    private static void ComporPaginaLivroCaixaResumo(IDocumentContainer container, string nomeCliente, int anoBase, int anoDeclaracao)
    {
        var linhas = new[]
        {
            new { Mes = "JANEIRO",   Receita =       0m,    Despesa =  11955.16m },
            new { Mes = "FEVEREIRO", Receita =  172974.00m, Despesa =  10123.73m },
            new { Mes = "MARÇO",     Receita =  294346.16m, Despesa = 133074.53m },
            new { Mes = "ABRIL",     Receita =       0m,    Despesa = 118264.07m },
            new { Mes = "MAIO",      Receita =       0m,    Despesa =   6795.23m },
            new { Mes = "JUNHO",     Receita =  257833.32m, Despesa =   7287.13m },
            new { Mes = "JULHO",     Receita =       0m,    Despesa = 319415.13m },
            new { Mes = "AGOSTO",    Receita =       0m,    Despesa =   8453.13m },
            new { Mes = "SETEMBRO",  Receita =       0m,    Despesa =  11898.75m },
            new { Mes = "OUTUBRO",   Receita =       0m,    Despesa =   7861.63m },
            new { Mes = "NOVEMBRO",  Receita =   18800.00m, Despesa =  11215.77m },
            new { Mes = "DEZEMBRO",  Receita =       0m,    Despesa =  22044.06m },
        };

        var totalReceitas = linhas.Sum(x => x.Receita);   // 743.953,48
        var totalDespesas = linhas.Sum(x => x.Despesa);   // 658.388,32

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            // margens fiéis: esquerda 37.8pt, direita 553.8pt → margem ~38pt cada lado
            page.MarginLeft(38);
            page.MarginRight(38);
            page.MarginTop(30);
            page.MarginBottom(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(18));

            // ── cabeçalho ──────────────────────────────────────────────────
            page.Header().Column(col =>
            {
                col.Spacing(0);

                // Nome do cliente centrado, fonte 18 bold
                col.Item().AlignCenter().Text(nomeCliente).Bold().FontSize(18);

                // Linha underline abaixo do nome (reproduz rect y=221.6)
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);

                // Título da página, fonte 14
                col.Item().PaddingTop(6).AlignCenter()
                    .Text($"DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE {anoBase}")
                    .FontSize(14);
            });

            // ── conteúdo: cabeçalho da tabela + linhas ─────────────────────
            page.Content().PaddingTop(14).ScaleToFit().Column(col =>
            {
                col.Item().Table(table =>
                {
                    // 4 colunas: mês | receita | despesa
                    // larguras aproximadas: col1=206pt, col2=154pt, col3=155pt
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(206);
                        cols.RelativeColumn(154);
                        cols.RelativeColumn(155);
                    });

                    // cabeçalho da tabela
                    table.Header(h =>
                    {
                        h.Cell().Element(c => CabecalhoTabela(c, isFirst: true)).Text(string.Empty);
                        h.Cell().Element(c => CabecalhoTabela(c)).AlignCenter().Text("RECEITA");
                        h.Cell().Element(c => CabecalhoTabela(c, isLast: true)).AlignCenter().Text("DESPESAS");
                    });

                    // linhas de meses
                    foreach (var linha in linhas)
                    {
                        table.Cell().Element(c => CelulaTabela(c, isFirst: true)).Text(linha.Mes);
                        table.Cell().Element(c => CelulaTabela(c)).AlignRight()
                            .Text(linha.Receita == 0m ? "0,00" : FormataNumero(linha.Receita));
                        table.Cell().Element(c => CelulaTabela(c, isLast: true)).AlignRight()
                            .Text(FormataNumero(linha.Despesa));
                    }

                    // linha TOTAL — fundo cinza (rgb 0.851)
                    table.Cell().Element(c => CelulaTotalBg(c, isFirst: true)).Text("TOTAL");
                    table.Cell().Element(c => CelulaTotalBg(c)).AlignRight().Text(FormataNumero(totalReceitas));
                    table.Cell().Element(c => CelulaTotalBg(c, isLast: true)).AlignRight().Text(FormataNumero(totalDespesas));
                });
            });
        });
    }

    // ─── PÁGINA 2: gráfico de barras VERTICAL — idêntico ao PDF de exemplo ──
    private static void ComporPaginaLivroCaixaConsolidado(IDocumentContainer container, string nomeCliente, int anoBase, int anoDeclaracao)
    {
        var totalReceitas = 743953.48m;
        var totalDespesas = 658388.32m;
        var margem        =  85565.16m;

        // alturas das barras (em pt) proporcionais à barra mais alta
        // No PDF: receita top=301.4 bot=616 → h=314.6; despesa top=337.7 bot=616 → h=278.3; margem top=579.7 bot=616 → h=36.3
        const float alturaMax = 314.6f;
        var alturaReceita  = 314.6f;
        var alturaDespesa  = 278.3f;
        var alturaMargem   =  36.3f;

        // cores extraídas do PDF:
        // receita: rgb(0, 0.690, 0.314) → #00B050
        // despesa: rgb(1, 0, 0) → red
        // margem:  rgb(0, 0.690, 0.941) → #00B0F0
        var corReceita = "#00B050";
        var corDespesa = "#FF0000";
        var corMargem  = "#00B0F0";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginLeft(38);
            page.MarginRight(38);
            page.MarginTop(30);
            page.MarginBottom(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(18));

            // ── cabeçalho igual à página 1 ─────────────────────────────────
            page.Header().Column(col =>
            {
                col.Spacing(0);
                col.Item().AlignCenter().Text(nomeCliente).Bold().FontSize(18);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().PaddingTop(6).AlignCenter()
                    .Text($"DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE {anoBase}")
                    .FontSize(14);
            });

            // Posicionamento absoluto com medidas fixas para evitar quebra/realinhamento do fluxo.
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
                            c.Item().AlignCenter().PaddingVertical(4)
                                .Text($"R$               {FormataNumero(totalReceitas)}").FontSize(11);
                        });

                        row.RelativeItem().Height(66).Border(1).BorderColor(Colors.Black).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingVertical(4).Text("DESPESAS").Bold().FontSize(16);
                            c.Item().LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().PaddingVertical(4)
                                .Text($"R$               {FormataNumero(totalDespesas)}").FontSize(11);
                        });

                        row.RelativeItem().Height(66).Border(1).BorderColor(Colors.Black).Column(c =>
                        {
                            c.Item().AlignCenter().PaddingVertical(4).Text("MARGEM").Bold().FontSize(16);
                            c.Item().LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().PaddingVertical(4)
                                .Text($"R$                 {FormataNumero(margem)}").FontSize(11);
                        });
                    });
                });
            });
        });
    }

    private static string FormataNumero(decimal valor)
        => string.Format(PtBr, "{0:N2}", valor);

    private static void ComporTabelaNotasFiscais(
        IDocumentContainer container,
        string nomeCliente,
        int anoBase,
        string titulo,
        IReadOnlyList<NotaFiscal> notas)
    {
        var totalNotas = notas.Sum(x => x.Valor);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(10));

            page.Header().Column(col =>
            {
                col.Spacing(3);
                col.Item().AlignCenter().Text(nomeCliente).SemiBold().FontSize(13);
                col.Item().AlignCenter().Text($"{titulo} - ANO BASE {anoBase}").SemiBold().FontSize(12);
                col.Item().LineHorizontal(1).LineColor(Colors.Black);
            });

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

                foreach (var nota in notas.OrderBy(x => x.DataEmissao))
                {
                    table.Cell().Element(TabelaNotaBody).Text(nota.DataEmissao.ToString("dd/MM/yyyy"));
                    table.Cell().Element(TabelaNotaBody).Text(nota.NomeOrigem);
                    table.Cell().Element(TabelaNotaBody).Text(nota.Descricao);
                    table.Cell().Element(TabelaNotaBody).AlignRight().Text($"R$ {FormataNumero(nota.Valor)}");
                }

                table.Cell().ColumnSpan(3).Element(TabelaNotaTotal).AlignRight().Text("Total das notas fiscais relacionadas");
                table.Cell().Element(TabelaNotaTotal).AlignRight().Text($"R$ {FormataNumero(totalNotas)}");
            });
        });
    }

    private static void ComporPaginasRebanhos(
        IDocumentContainer container,
        string nomeCliente,
        int anoBase,
        IReadOnlyList<ResumoRebanho> rebanhos)
    {
        var totalNascimentos = rebanhos.Sum(x => x.Nascimentos);
        var totalMortesConsumo = rebanhos.Sum(x => x.MortesConsumo);
        var totalEntradas = rebanhos.Sum(x => x.Entradas);
        var totalSaidas = rebanhos.Sum(x => x.Saidas);

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Arial).FontSize(10));

            page.Header().Column(col =>
            {
                col.Spacing(3);
                col.Item().AlignCenter().Text(nomeCliente).SemiBold().FontSize(13);
                col.Item().AlignCenter().Text($"RELATORIO DE REBANHOS - ANO BASE {anoBase}").SemiBold().FontSize(12);
                col.Item().LineHorizontal(1).LineColor(Colors.Black);
            });

            // Um único fluxo paginado: o QuestPDF coloca automaticamente o máximo de rebanhos por página.
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(8);

                foreach (var rebanho in rebanhos)
                {
                    col.Item().ShowEntire().Border(1).BorderColor(Colors.Black).Padding(6).Column(bloco =>
                    {
                        bloco.Spacing(4);

                        bloco.Item().Text($"Nome do Rebanho: {rebanho.Nome}").SemiBold();
                        bloco.Item().Text($"Inscricao do Rebanho: {rebanho.Inscricao}").SemiBold();

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
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(rebanho.Nascimentos.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Mortes/Consumo");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(rebanho.MortesConsumo.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Entradas");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(rebanho.Entradas.ToString(PtBr));

                            table.Cell().Element(TabelaRebanhoBody).Text("Saidas");
                            table.Cell().Element(TabelaRebanhoBody).AlignRight().Text(rebanho.Saidas.ToString(PtBr));
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

    // cabeçalho da tabela: sem bordas laterais, apenas linha preta inferior grossa
    private static IContainer CabecalhoTabela(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .PaddingVertical(6).PaddingHorizontal(4)
            .DefaultTextStyle(t => t.Bold().FontSize(18));
    }

    // célula normal: apenas linha preta inferior (1pt) — igual ao PDF
    private static IContainer CelulaTabela(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .PaddingVertical(5).PaddingHorizontal(4)
            .DefaultTextStyle(t => t.FontSize(18));
    }

    // célula linha TOTAL: fundo cinza (0.851) + linha preta inferior + superior
    private static IContainer CelulaTotalBg(IContainer c, bool isFirst = false, bool isLast = false)
    {
        var cell = c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Black);

        if (isFirst)
            cell = cell.BorderLeft(1).BorderColor(Colors.Black);

        cell = cell.BorderRight(1).BorderColor(Colors.Black);

        return cell
            .Background("#D9D9D9")
            .PaddingVertical(5).PaddingHorizontal(4)
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

    private sealed record NotaFiscal(DateTime DataEmissao, string NomeOrigem, string Descricao, decimal Valor);
    private sealed record ResumoRebanho(string Nome, string Inscricao, int Nascimentos, int MortesConsumo, int Entradas, int Saidas);
}