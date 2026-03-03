using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioFinanceiroService : IRelatorioFinanceiroService
{
    private static readonly System.Globalization.CultureInfo PtBr = new("pt-BR");

    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RelatorioFinanceiroService(ILancamentoRepository lancamentoRepository, IProdutorRepository produtorRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _produtorRepository = produtorRepository;
    }

    public byte[] GerarRelatorioFinanceiro(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo)
    {
        return GerarRelatorioFinanceiroAsync(dataInicial, dataFinal, tipo, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task<byte[]> GerarRelatorioFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, CancellationToken cancellationToken)
    {
        var inicio = dataInicial.Date;
        var fim = dataFinal.Date.AddDays(1).AddTicks(-1);

        var produtor = (await _produtorRepository.GetAllAsync())
            .OrderBy(item => item.Nome)
            .FirstOrDefault();

        var total = await CalcularTotalAsync(inicio, fim, tipo, produtor?.Id, cancellationToken);

        var modelo = new RelatorioFinanceiroDto
        {
            NomeProdutor = produtor?.Nome ?? "PRODUTOR NÃO INFORMADO",
            DataInicial = inicio,
            DataFinal = dataFinal.Date,
            DataGeracao = DateTime.Now,
            Tipo = tipo,
            Total = total
        };

        await using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new PageNumberEventHandler());

        using var document = new Document(pdf);
        var (fonteTitulo, fonteNormal) = CriarFontes();

        AdicionarCabecalho(document, modelo, fonteTitulo, fonteNormal);
        var tabela = await MontarTabelaAsync(modelo, produtor?.Id, fonteTitulo, fonteNormal, cancellationToken);
        document.Add(tabela);

        var labelTotal = modelo.Tipo == TipoLancamento.Receita ? "Total das Receitas" : "Total das Despesas";
        document.Add(new Paragraph($"{labelTotal}: {FormatarMoeda(modelo.Total)}")
            .SetFont(fonteTitulo)
            .SetMarginTop(12));

        document.Close();
        return stream.ToArray();
    }

    private static void AdicionarCabecalho(Document document, RelatorioFinanceiroDto modelo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(modelo.Tipo == TipoLancamento.Receita ? "Relatório de Receitas" : "Relatório de Despesas")
            .SetFont(fonteTitulo)
            .SetFontSize(16)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(12));

        document.Add(new Paragraph($"Data: {modelo.DataGeracao:dd/MM/yy}").SetFont(fonteNormal).SetFontSize(9));
        document.Add(new Paragraph($"Hora: {modelo.DataGeracao:HH:mm}").SetFont(fonteNormal).SetFontSize(9));

        document.Add(new Paragraph(modelo.NomeProdutor)
            .SetFont(fonteTitulo)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(8));

        document.Add(new Paragraph(modelo.Tipo == TipoLancamento.Receita ? "RELATÓRIO DE RECEITAS" : "RELATÓRIO DE DESPESAS")
            .SetFont(fonteTitulo)
            .SetFontSize(13)
            .SetTextAlignment(TextAlignment.CENTER));

        document.Add(new Paragraph($"Período de {modelo.DataInicial:dd/MM/yy} a {modelo.DataFinal:dd/MM/yy}")
            .SetFont(fonteNormal)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));
    }

    private async Task<Table> MontarTabelaAsync(RelatorioFinanceiroDto modelo, int? produtorId, PdfFont fonteTitulo, PdfFont fonteNormal, CancellationToken cancellationToken)
    {
        var colunaParceiro = modelo.Tipo == TipoLancamento.Receita ? "Cliente" : "Fornecedor";
        var colunaDescricao = modelo.Tipo == TipoLancamento.Receita ? "Descrição das Receitas" : "Descrição das Despesas";

        var tabela = PdfTableBuilder.CriarTabela(
            [1.3f, 2.2f, 4f, 1.5f],
            ["Data", colunaParceiro, colunaDescricao, "Valor"],
            fonteTitulo);

        await foreach (var item in _lancamentoRepository.StreamFinanceiroAsync(modelo.DataInicial, modelo.DataFinal.AddDays(1).AddTicks(-1), modelo.Tipo, produtorId, null, cancellationToken))
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

        return tabela;
    }

    private async Task<decimal> CalcularTotalAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId, CancellationToken cancellationToken)
    {
        return await _lancamentoRepository.ObterTotalFinanceiroAsync(dataInicial, dataFinal, tipo, produtorId, null, cancellationToken);
    }

    private static string FormatarMoeda(decimal valor)
    {
        return string.Format(PtBr, "{0:C}", valor);
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

    private sealed class PageNumberEventHandler : IEventHandler
    {
        public void HandleEvent(Event currentEvent)
        {
            var documentEvent = (PdfDocumentEvent)currentEvent;
            var pdf = documentEvent.GetDocument();
            var page = documentEvent.GetPage();
            var pageNumber = pdf.GetPageNumber(page);
            var pageSize = page.GetPageSize();

            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdf);
            using var layoutCanvas = new Canvas(canvas, new Rectangle(pageSize.GetLeft(), pageSize.GetBottom(), pageSize.GetWidth(), pageSize.GetHeight()));

            layoutCanvas.ShowTextAligned(
                new Paragraph($"Página {pageNumber}").SetFontSize(9),
                pageSize.GetWidth() / 2,
                18,
                TextAlignment.CENTER);
        }
    }
}
