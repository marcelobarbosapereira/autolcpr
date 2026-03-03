using System.Globalization;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SkiaSharp;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioAnualService : IRelatorioAnualService
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private static readonly string[] TiposRebanhoPadrao = ["Nascimentos", "Compras", "Vendas", "Óbitos"];

    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IMovimentacaoRebanhoRepository _movimentacaoRebanhoRepository;
    private readonly IRebanhoRepository _rebanhoRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RelatorioAnualService(
        ILancamentoRepository lancamentoRepository,
        IMovimentacaoRebanhoRepository movimentacaoRebanhoRepository,
        IRebanhoRepository rebanhoRepository,
        IProdutorRepository produtorRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _movimentacaoRebanhoRepository = movimentacaoRebanhoRepository;
        _rebanhoRepository = rebanhoRepository;
        _produtorRepository = produtorRepository;
    }

    public byte[] GerarRelatorioAnual(int anoFiscal)
    {
        return GerarRelatorioAnualAsync(anoFiscal, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task<byte[]> GerarRelatorioAnualAsync(int anoFiscal, CancellationToken cancellationToken)
    {
        var dadosColetados = await ColetarDadosAsync(anoFiscal, cancellationToken);
        var modelo = MontarModelo(dadosColetados, anoFiscal);

        try
        {
            return await GerarPdfAsync(modelo, dadosColetados.ProdutorId, cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                return GerarPdfFallback(modelo, ex);
            }
            catch (Exception fallbackEx)
            {
                throw new InvalidOperationException($"Falha ao gerar PDF completo ({ex.Message}) e também ao gerar fallback ({fallbackEx.Message}).", fallbackEx);
            }
        }
    }

    private async Task<RelatorioAnualDadosColetados> ColetarDadosAsync(int anoFiscal, CancellationToken cancellationToken)
    {
        var (dataInicio, dataFim) = ResolverPeriodo(anoFiscal);

        var produtor = (await _produtorRepository.GetAllAsync())
            .OrderBy(item => item.Nome)
            .FirstOrDefault();

        var resumoMensal = await _lancamentoRepository.ObterResumoMensalAsync(dataInicio, dataFim, produtor?.Id, cancellationToken);
        var totalReceitas = await _lancamentoRepository.ObterTotalPorTipoAsync(dataInicio, dataFim, TipoLancamento.Receita, produtor?.Id, cancellationToken);
        var totalDespesas = await _lancamentoRepository.ObterTotalPorTipoAsync(dataInicio, dataFim, TipoLancamento.Despesa, produtor?.Id, cancellationToken);
        var rebanhos = produtor?.Id is int produtorId
            ? (await _rebanhoRepository.GetByProdutorIdAsync(produtorId)).ToList()
            : (await _rebanhoRepository.GetAllAsync()).ToList();

        var propriedades = rebanhos
            .OrderBy(item => item.NomeRebanho)
            .Select(item => new PropriedadeRelatorioDto
            {
                NomePropriedade = item.NomeRebanho,
                InscricaoPropriedade = item.IdRebanho,
                TotalNascimentos = item.Nascimentos,
                TotalCompras = item.Entradas,
                TotalVendas = item.Saidas,
                TotalObitos = item.Mortes
            })
            .DistinctBy(item => new { item.NomePropriedade, item.InscricaoPropriedade })
            .ToList();

        if (propriedades.Count == 0)
        {
            propriedades.Add(new PropriedadeRelatorioDto
            {
                NomePropriedade = "NÃO INFORMADA",
                InscricaoPropriedade = "NÃO INFORMADA",
                TotalNascimentos = 0,
                TotalCompras = 0,
                TotalVendas = 0,
                TotalObitos = 0
            });
        }

        var resumoRebanhoMovimentacao = await _movimentacaoRebanhoRepository.ObterResumoPorTipoAsync(dataInicio, dataFim, produtor?.Id, cancellationToken);
        var resumoRebanho = ResolverResumoRebanho(resumoRebanhoMovimentacao, rebanhos);

        var possuiFinanceiro = totalReceitas != 0m
                              || totalDespesas != 0m
                              || resumoMensal.Any(item => item.Receita != 0m || item.Despesa != 0m);

        var possuiRebanho = resumoRebanho.Any(item => item.Quantidade != 0);

        if (!possuiFinanceiro && !possuiRebanho)
        {
            throw new InvalidOperationException($"Não há dados para gerar o Livro Caixa no período de {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}.");
        }

        return new RelatorioAnualDadosColetados
        {
            NomeProdutor = produtor?.Nome ?? "PRODUTOR NÃO INFORMADO",
            Propriedades = propriedades,
            ProdutorId = produtor?.Id,
            DataInicio = dataInicio,
            DataFim = dataFim,
            ResumoMensal = resumoMensal,
            TotalReceitas = totalReceitas,
            TotalDespesas = totalDespesas,
            ResumoRebanho = resumoRebanho
        };
    }

    private static IReadOnlyList<ResumoMovimentacaoRebanho> ResolverResumoRebanho(
        IReadOnlyList<ResumoMovimentacaoRebanho> resumoMovimentacao,
        IReadOnlyList<Rebanho> rebanhos)
    {
        if (resumoMovimentacao.Any(item => item.Quantidade != 0))
        {
            return resumoMovimentacao;
        }

        var totalNascimentos = rebanhos.Sum(item => item.Nascimentos);
        var totalCompras = rebanhos.Sum(item => item.Entradas);
        var totalVendas = rebanhos.Sum(item => item.Saidas);
        var totalObitos = rebanhos.Sum(item => item.Mortes);

        return new List<ResumoMovimentacaoRebanho>
        {
            new() { TipoMovimentacao = "Nascimentos", Quantidade = totalNascimentos },
            new() { TipoMovimentacao = "Compras", Quantidade = totalCompras },
            new() { TipoMovimentacao = "Vendas", Quantidade = totalVendas },
            new() { TipoMovimentacao = "Óbitos", Quantidade = totalObitos }
        };
    }

    private static RelatorioAnualDto MontarModelo(RelatorioAnualDadosColetados dados, int anoFiscal)
    {
        var resumoMensal = dados.ResumoMensal
            .OrderBy(item => item.Mes)
            .Select(item => new ResumoMensalDto
            {
                Mes = item.Mes,
                NomeMes = PtBr.DateTimeFormat.GetMonthName(item.Mes),
                Receita = item.Receita,
                Despesa = item.Despesa
            })
            .ToList();

        var rebanhoMap = dados.ResumoRebanho
            .ToDictionary(item => item.TipoMovimentacao, item => item.Quantidade, StringComparer.OrdinalIgnoreCase);

        var resumoRebanho = TiposRebanhoPadrao
            .Select(tipo => new ResumoRebanhoDto
            {
                TipoMovimentacao = tipo,
                Quantidade = rebanhoMap.TryGetValue(tipo, out var quantidade) ? quantidade : 0
            })
            .ToList();

        foreach (var adicional in dados.ResumoRebanho.Where(item => TiposRebanhoPadrao.All(tipo => !tipo.Equals(item.TipoMovimentacao, StringComparison.OrdinalIgnoreCase))))
        {
            resumoRebanho.Add(new ResumoRebanhoDto
            {
                TipoMovimentacao = adicional.TipoMovimentacao,
                Quantidade = adicional.Quantidade
            });
        }

        return new RelatorioAnualDto
        {
            NomeProdutor = dados.NomeProdutor,
            Propriedades = dados.Propriedades,
            AnoExercicio = anoFiscal + 1,
            AnoBase = anoFiscal,
            DataInicio = dados.DataInicio,
            DataFim = dados.DataFim,
            ResumoMensal = resumoMensal,
            TotalReceitas = dados.TotalReceitas,
            TotalDespesas = dados.TotalDespesas,
            MargemAnual = dados.TotalReceitas - dados.TotalDespesas,
            ResumoRebanho = resumoRebanho
        };
    }

    private async Task<byte[]> GerarPdfAsync(RelatorioAnualDto modelo, int? produtorId, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        var (fonteTitulo, fonteNormal) = CriarFontes();

        AdicionarCapaInicial(document, modelo, fonteTitulo, fonteNormal);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        AdicionarDemonstrativoAnual(document, modelo, fonteTitulo, fonteNormal);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        await AdicionarSecaoReceitasAsync(document, modelo, produtorId, fonteTitulo, fonteNormal, cancellationToken);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        await AdicionarSecaoDespesasAsync(document, modelo, produtorId, fonteTitulo, fonteNormal, cancellationToken);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        AdicionarSecaoRebanho(document, modelo, fonteTitulo, fonteNormal);

        document.Close();
        return stream.ToArray();
    }

    private static void AdicionarCapaInicial(Document document, RelatorioAnualDto modelo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(SanitizarTexto(modelo.NomeProdutor))
            .SetFont(fonteTitulo)
            .SetFontSize(18)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(180));

        document.Add(new Paragraph("LIVRO CAIXA")
            .SetFont(fonteTitulo)
            .SetFontSize(30)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(30));

        document.Add(new Paragraph($"EX: {modelo.AnoExercicio}")
            .SetFont(fonteNormal)
            .SetFontSize(16)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(25));

        document.Add(new Paragraph($"ANO BASE: {modelo.AnoBase}")
            .SetFont(fonteNormal)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(10));
    }

    private static void AdicionarDemonstrativoAnual(Document document, RelatorioAnualDto modelo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph("DEMONSTRATIVO DE RECEITAS E DESPESAS - ANO BASE")
            .SetFont(fonteTitulo)
            .SetFontSize(16)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(20));

        var tabela = new Table(UnitValue.CreatePercentArray([3, 2, 2])).UseAllAvailableWidth();
        AdicionarCabecalhoTabela(tabela, "Mês", "Receita", "Despesa");

        foreach (var item in modelo.ResumoMensal)
        {
            tabela.AddCell(CriarCelula(item.NomeMes, fonteNormal));
            tabela.AddCell(CriarCelula(FormatarMoeda(item.Receita), fonteNormal, TextAlignment.RIGHT));
            tabela.AddCell(CriarCelula(FormatarMoeda(item.Despesa), fonteNormal, TextAlignment.RIGHT));
        }

        tabela.AddCell(CriarCelulaDestaque("TOTAL", fonteTitulo));
        tabela.AddCell(CriarCelulaDestaque(FormatarMoeda(modelo.TotalReceitas), fonteTitulo, TextAlignment.RIGHT));
        tabela.AddCell(CriarCelulaDestaque(FormatarMoeda(modelo.TotalDespesas), fonteTitulo, TextAlignment.RIGHT));

        document.Add(tabela);

        document.Add(new Paragraph($"RECEITA: {FormatarMoeda(modelo.TotalReceitas)}").SetFont(fonteNormal).SetMarginTop(18));
        document.Add(new Paragraph($"DESPESAS: {FormatarMoeda(modelo.TotalDespesas)}").SetFont(fonteNormal));
        document.Add(new Paragraph($"MARGEM: {FormatarMoeda(modelo.MargemAnual)}").SetFont(fonteTitulo));

        try
        {
            var graficoBytes = CriarImagemGraficoComparativo(modelo.ResumoMensal);
            var grafico = new Image(ImageDataFactory.Create(graficoBytes))
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginTop(16);

            document.Add(grafico);
        }
        catch
        {
            document.Add(new Paragraph("Gráfico comparativo indisponível para este relatório.")
                .SetFont(fonteNormal)
                .SetFontSize(9)
                .SetMarginTop(10));
        }
    }

    private async Task AdicionarSecaoReceitasAsync(Document document, RelatorioAnualDto modelo, int? produtorId, PdfFont fonteTitulo, PdfFont fonteNormal, CancellationToken cancellationToken)
    {
        await AdicionarSecaoLancamentosAsync(
            document,
            modelo,
            produtorId,
            TipoLancamento.Receita,
            "RECEITAS",
            "RELATÓRIO DE RECEITAS",
            "Cliente",
            "Descrição das Receitas",
            "Total das Receitas",
            modelo.TotalReceitas,
            fonteTitulo,
            fonteNormal,
            cancellationToken);
    }

    private async Task AdicionarSecaoDespesasAsync(Document document, RelatorioAnualDto modelo, int? produtorId, PdfFont fonteTitulo, PdfFont fonteNormal, CancellationToken cancellationToken)
    {
        await AdicionarSecaoLancamentosAsync(
            document,
            modelo,
            produtorId,
            TipoLancamento.Despesa,
            "DESPESAS",
            "RELATÓRIO DE DESPESAS",
            "Fornecedor",
            "Descrição das Despesas",
            "Total das Despesas",
            modelo.TotalDespesas,
            fonteTitulo,
            fonteNormal,
            cancellationToken);
    }

    private async Task AdicionarSecaoLancamentosAsync(
        Document document,
        RelatorioAnualDto modelo,
        int? produtorId,
        TipoLancamento tipoLancamento,
        string tituloSecao,
        string tituloRelatorio,
        string colunaParceiro,
        string colunaDescricao,
        string labelTotal,
        decimal total,
        PdfFont fonteTitulo,
        PdfFont fonteNormal,
        CancellationToken cancellationToken)
    {
        AdicionarCabecalhoSecao(document, modelo, tituloSecao, tituloRelatorio, fonteTitulo, fonteNormal);

        var tabela = PdfTableBuilder.CriarTabela(
            [1.3f, 2.2f, 4f, 1.5f],
            ["Data", colunaParceiro, colunaDescricao, "Valor"],
            fonteTitulo);

        await foreach (var item in _lancamentoRepository.StreamPorTipoAsync(modelo.DataInicio, modelo.DataFim, tipoLancamento, produtorId, cancellationToken))
        {
            PdfTableBuilder.AdicionarLinha(
                tabela,
                [
                    item.Data.ToString("dd/MM/yyyy"),
                    item.ClienteFornecedor,
                    item.Descricao,
                    FormatarMoeda(item.Valor)
                ],
                fonteNormal,
                [TextAlignment.LEFT, TextAlignment.LEFT, TextAlignment.LEFT, TextAlignment.RIGHT]);
        }

        document.Add(tabela);
        document.Add(new Paragraph($"{labelTotal}: {FormatarMoeda(total)}")
            .SetFont(fonteTitulo)
            .SetMarginTop(12));
    }

    private static void AdicionarSecaoRebanho(Document document, RelatorioAnualDto modelo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(SanitizarTexto(modelo.NomeProdutor))
            .SetFont(fonteTitulo)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph("REBANHOS")
            .SetFont(fonteTitulo)
            .SetFontSize(20)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"({modelo.DataInicio:dd/MM/yyyy} a {modelo.DataFim:dd/MM/yyyy})")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"EX: {modelo.AnoExercicio}")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"ANO BASE: {modelo.AnoBase}")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(18));

        foreach (var propriedade in modelo.Propriedades)
        {
            document.Add(new Paragraph()
                .Add(new Text("Nome da propriedade: ").SetFont(fonteTitulo))
                .Add(new Text(SanitizarTexto(propriedade.NomePropriedade)).SetFont(fonteNormal))
                .SetMarginBottom(2));
            document.Add(new Paragraph($"Inscrição: {SanitizarTexto(propriedade.InscricaoPropriedade)}")
                .SetFont(fonteNormal)
                .SetMarginBottom(8));

            var tabela = new Table(UnitValue.CreatePercentArray([4, 2])).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "", "Quantidade");

            tabela.AddCell(CriarCelula("Nascimentos", fonteNormal));
            tabela.AddCell(CriarCelula(propriedade.TotalNascimentos.ToString(PtBr), fonteNormal, TextAlignment.RIGHT));

            tabela.AddCell(CriarCelula("Compras", fonteNormal));
            tabela.AddCell(CriarCelula(propriedade.TotalCompras.ToString(PtBr), fonteNormal, TextAlignment.RIGHT));

            tabela.AddCell(CriarCelula("Vendas", fonteNormal));
            tabela.AddCell(CriarCelula(propriedade.TotalVendas.ToString(PtBr), fonteNormal, TextAlignment.RIGHT));

            tabela.AddCell(CriarCelula("Óbitos", fonteNormal));
            tabela.AddCell(CriarCelula(propriedade.TotalObitos.ToString(PtBr), fonteNormal, TextAlignment.RIGHT));

            document.Add(tabela);
            document.Add(new Paragraph(" ").SetMarginBottom(4));
        }

        var totalNascimentos = modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Nascimentos", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalCompras = modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Compras", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalVendas = modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Vendas", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var totalObitos = modelo.ResumoRebanho
            .FirstOrDefault(item => item.TipoMovimentacao.Equals("Óbitos", StringComparison.OrdinalIgnoreCase))
            ?.Quantidade ?? 0;
        var saldoRebanhoAno = totalNascimentos + totalCompras - totalVendas - totalObitos;

        document.Add(new Paragraph(" "));
        document.Add(new Paragraph($"Total de Nascimentos: {totalNascimentos}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Compras: {totalCompras}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Vendas: {totalVendas}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Óbitos: {totalObitos}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Saldo do Rebanho no Ano: {saldoRebanhoAno}")
            .SetFont(fonteTitulo)
            .SetMarginTop(4));
    }

    private static void AdicionarCabecalhoSecao(Document document, RelatorioAnualDto modelo, string tituloSecao, string tituloRelatorio, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(SanitizarTexto(modelo.NomeProdutor))
            .SetFont(fonteTitulo)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph(tituloSecao)
            .SetFont(fonteTitulo)
            .SetFontSize(20)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"({modelo.DataInicio:dd/MM/yyyy} a {modelo.DataFim:dd/MM/yyyy})")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"EX: {modelo.AnoExercicio}")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph($"ANO BASE: {modelo.AnoBase}")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(14));

        document.Add(new Paragraph($"Data: {DateTime.Now:dd/MM/yy}").SetFont(fonteNormal).SetFontSize(9));
        document.Add(new Paragraph($"Hora: {DateTime.Now:HH:mm}").SetFont(fonteNormal).SetFontSize(9));
        document.Add(new Paragraph(tituloRelatorio)
            .SetFont(fonteTitulo)
            .SetFontSize(13)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(8)
            .SetMarginBottom(10));
    }

    private static void AdicionarCabecalhoTabela(Table tabela, params string[] cabecalhos)
    {
        foreach (var cabecalho in cabecalhos)
        {
            tabela.AddHeaderCell(new Cell()
                .Add(new Paragraph(cabecalho).SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(235, 235, 235))
                .SetBorder(new SolidBorder(new DeviceRgb(180, 180, 180), 0.8f))
                .SetTextAlignment(TextAlignment.CENTER));
        }
    }

    private static Cell CriarCelula(string texto, PdfFont fonte, TextAlignment alinhamento = TextAlignment.LEFT)
    {
        return new Cell()
            .Add(new Paragraph(SanitizarTexto(texto)).SetFont(fonte).SetFontSize(9))
            .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
            .SetTextAlignment(alinhamento);
    }

    private static Cell CriarCelulaDestaque(string texto, PdfFont fonte, TextAlignment alinhamento = TextAlignment.LEFT)
    {
        return new Cell()
            .Add(new Paragraph(SanitizarTexto(texto)).SetFont(fonte).SetFontSize(10))
            .SetBackgroundColor(new DeviceRgb(245, 245, 245))
            .SetBorder(new SolidBorder(new DeviceRgb(160, 160, 160), 0.9f))
            .SetTextAlignment(alinhamento);
    }

    private static string FormatarMoeda(decimal valor)
    {
        return string.Format(PtBr, "{0:C}", valor);
    }

    private static byte[] CriarImagemGraficoComparativo(IReadOnlyList<ResumoMensalDto> dados)
    {
        const int largura = 1000;
        const int altura = 380;

        using var superficie = SKSurface.Create(new SKImageInfo(largura, altura));
        var canvas = superficie.Canvas;
        canvas.Clear(SKColors.White);

        var margemEsquerda = 70f;
        var margemDireita = 30f;
        var margemTopo = 35f;
        var margemInferior = 55f;

        var areaLargura = largura - margemEsquerda - margemDireita;
        var areaAltura = altura - margemTopo - margemInferior;

        using var linhaPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f, IsAntialias = true };
        using var textoPaint = new SKPaint { Color = new SKColor(70, 70, 70), TextSize = 16f, IsAntialias = true };
        using var receitaPaint = new SKPaint { Color = new SKColor(34, 139, 34), IsAntialias = true };
        using var despesaPaint = new SKPaint { Color = new SKColor(220, 53, 69), IsAntialias = true };

        canvas.DrawLine(margemEsquerda, margemTopo, margemEsquerda, margemTopo + areaAltura, linhaPaint);
        canvas.DrawLine(margemEsquerda, margemTopo + areaAltura, margemEsquerda + areaLargura, margemTopo + areaAltura, linhaPaint);

        var maxValor = dados.Any() ? dados.Max(item => Math.Max(item.Receita, item.Despesa)) : 0m;
        if (maxValor <= 0)
        {
            maxValor = 1m;
        }

        var slot = areaLargura / 12f;
        var barraLargura = Math.Max(8f, slot * 0.28f);

        for (var i = 0; i < 12; i++)
        {
            var item = dados.FirstOrDefault(x => x.Mes == i + 1) ?? new ResumoMensalDto { Mes = i + 1, NomeMes = PtBr.DateTimeFormat.GetAbbreviatedMonthName(i + 1), Receita = 0m, Despesa = 0m };
            var centro = margemEsquerda + (slot * i) + (slot / 2f);

            var alturaReceita = (float)(item.Receita / maxValor) * areaAltura;
            var alturaDespesa = (float)(item.Despesa / maxValor) * areaAltura;

            var receitaRect = SKRect.Create(centro - barraLargura - 2, margemTopo + areaAltura - alturaReceita, barraLargura, alturaReceita);
            var despesaRect = SKRect.Create(centro + 2, margemTopo + areaAltura - alturaDespesa, barraLargura, alturaDespesa);

            canvas.DrawRect(receitaRect, receitaPaint);
            canvas.DrawRect(despesaRect, despesaPaint);

            var nomeMesCurto = PtBr.DateTimeFormat.GetAbbreviatedMonthName(i + 1).Replace(".", "");
            canvas.DrawText(nomeMesCurto, centro - 13, altura - 22, textoPaint);
        }

        canvas.DrawText("Receitas", largura - 210, 30, textoPaint);
        canvas.DrawRect(SKRect.Create(largura - 285, 18, 22, 12), receitaPaint);
        canvas.DrawText("Despesas", largura - 110, 30, textoPaint);
        canvas.DrawRect(SKRect.Create(largura - 185, 18, 22, 12), despesaPaint);

        using var imagem = superficie.Snapshot();
        using var data = imagem.Encode(SKEncodedImageFormat.Png, 100);
        if (data == null)
        {
            throw new InvalidOperationException("Falha ao codificar o gráfico em PNG.");
        }

        return data.ToArray();
    }

    private static (PdfFont fonteTitulo, PdfFont fonteNormal) CriarFontes()
    {
        try
        {
            var fonteNormal = PdfFontFactory.CreateFont(@"C:\Windows\Fonts\arial.ttf", PdfEncodings.IDENTITY_H);
            var fonteTitulo = PdfFontFactory.CreateFont(@"C:\Windows\Fonts\arialbd.ttf", PdfEncodings.IDENTITY_H);
            return (fonteTitulo, fonteNormal);
        }
        catch
        {
            var fonteTitulo = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fonteNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            return (fonteTitulo, fonteNormal);
        }
    }

    private static (DateTime dataInicio, DateTime dataFim) ResolverPeriodo(int anoFiscal, DateTime? dataInicioCustom = null, DateTime? dataFimCustom = null)
    {
        if (dataInicioCustom.HasValue && dataFimCustom.HasValue)
        {
            return (dataInicioCustom.Value.Date, dataFimCustom.Value.Date);
        }

        var dataInicio = new DateTime(anoFiscal, 1, 1);
        var dataFim = new DateTime(anoFiscal, 12, 31, 23, 59, 59);
        return (dataInicio, dataFim);
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

    private static byte[] GerarPdfFallback(RelatorioAnualDto modelo, Exception ex)
    {
        using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        var fonteTitulo = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var fonteNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        document.Add(new Paragraph("LIVRO CAIXA - RELATÓRIO ANUAL")
            .SetFont(fonteTitulo)
            .SetFontSize(18)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(18));

        document.Add(new Paragraph($"Produtor: {SanitizarTexto(modelo.NomeProdutor)}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Ano Base: {modelo.AnoBase}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Exercício: {modelo.AnoExercicio}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Receita Total: {FormatarMoeda(modelo.TotalReceitas)}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Despesa Total: {FormatarMoeda(modelo.TotalDespesas)}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Margem: {FormatarMoeda(modelo.MargemAnual)}").SetFont(fonteNormal));
        document.Add(new Paragraph(" "));
        document.Add(new Paragraph("Obs: O layout completo não pôde ser renderizado e foi gerada uma versão simplificada.")
            .SetFont(fonteNormal)
            .SetFontSize(9));
        document.Add(new Paragraph($"Detalhe técnico: {SanitizarTexto(ex.Message)}")
            .SetFont(fonteNormal)
            .SetFontSize(8));

        document.Close();
        return stream.ToArray();
    }

    private sealed class RelatorioAnualDadosColetados
    {
        public required string NomeProdutor { get; init; }
        public required IReadOnlyList<PropriedadeRelatorioDto> Propriedades { get; init; }
        public int? ProdutorId { get; init; }
        public DateTime DataInicio { get; init; }
        public DateTime DataFim { get; init; }
        public required IReadOnlyList<ResumoMensalFinanceiro> ResumoMensal { get; init; }
        public decimal TotalReceitas { get; init; }
        public decimal TotalDespesas { get; init; }
        public required IReadOnlyList<ResumoMovimentacaoRebanho> ResumoRebanho { get; init; }
    }
}
