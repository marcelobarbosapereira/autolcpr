using AutoLCPR.Domain.Repositories;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioRebanhoService : IRelatorioRebanhoService
{
    private static readonly System.Globalization.CultureInfo PtBr = new("pt-BR");

    private readonly IRebanhoRepository _rebanhoRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RelatorioRebanhoService(
        IRebanhoRepository rebanhoRepository,
        IProdutorRepository produtorRepository)
    {
        _rebanhoRepository = rebanhoRepository;
        _produtorRepository = produtorRepository;
    }

    public byte[] GerarRelatorioRebanho(int ano)
    {
        return GerarRelatorioRebanhoAsync(ano, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task<byte[]> GerarRelatorioRebanhoAsync(int ano, CancellationToken cancellationToken)
    {
        var (dataInicio, dataFim) = ResolverPeriodo(ano);

        var produtor = (await _produtorRepository.GetAllAsync())
            .OrderBy(item => item.Nome)
            .FirstOrDefault();

        var rebanhos = produtor?.Id is int produtorId
            ? (await _rebanhoRepository.GetByProdutorIdAsync(produtorId)).ToList()
            : (await _rebanhoRepository.GetAllAsync()).ToList();

        var propriedades = rebanhos
            .OrderBy(item => item.NomeRebanho)
            .Select(item => new PropriedadeRebanhoDto
            {
                NomePropriedade = item.NomeRebanho,
                InscricaoPropriedade = item.IdRebanho,
                TotalNascimentos = item.Nascimentos,
                TotalCompras = item.Entradas,
                TotalVendas = item.Saidas,
                TotalObitos = item.Mortes
            })
            .ToList();

        if (propriedades.Count == 0)
        {
            propriedades.Add(new PropriedadeRebanhoDto
            {
                NomePropriedade = "NÃO INFORMADA",
                InscricaoPropriedade = "NÃO INFORMADA",
                TotalNascimentos = 0,
                TotalCompras = 0,
                TotalVendas = 0,
                TotalObitos = 0
            });
        }

        var resumoConsolidado = new ResumoRebanhoAnualDto
        {
            TotalNascimentos = propriedades.Sum(item => item.TotalNascimentos),
            TotalCompras = propriedades.Sum(item => item.TotalCompras),
            TotalVendas = propriedades.Sum(item => item.TotalVendas),
            TotalObitos = propriedades.Sum(item => item.TotalObitos),
            SaldoRebanhoAno = propriedades.Sum(item => item.TotalNascimentos + item.TotalCompras - item.TotalVendas - item.TotalObitos)
        };

        var modelo = new RelatorioRebanhoDto
        {
            NomeProdutor = produtor?.Nome ?? "PRODUTOR NÃO INFORMADO",
            AnoExercicio = ano + 1,
            AnoBase = ano,
            DataInicio = dataInicio,
            DataFim = dataFim,
            DataGeracao = DateTime.Now,
            Propriedades = propriedades,
            Resumo = new ResumoRebanhoAnualDto
            {
                TotalNascimentos = resumoConsolidado.TotalNascimentos,
                TotalCompras = resumoConsolidado.TotalCompras,
                TotalVendas = resumoConsolidado.TotalVendas,
                TotalObitos = resumoConsolidado.TotalObitos,
                SaldoRebanhoAno = resumoConsolidado.SaldoRebanhoAno
            }
        };

        await using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        var (fonteTitulo, fonteNormal) = CriarFontes();

        AdicionarCabecalhoPrincipal(document, modelo, fonteTitulo, fonteNormal);
        AdicionarCabecalhoTecnico(document, modelo.DataGeracao, fonteTitulo, fonteNormal);

        foreach (var propriedade in modelo.Propriedades)
        {
            document.Add(new Paragraph()
                .Add(new Text("Nome da propriedade: ").SetFont(fonteTitulo))
                .Add(new Text(propriedade.NomePropriedade).SetFont(fonteNormal))
                .SetMarginBottom(2));
            document.Add(new Paragraph($"Inscrição: {propriedade.InscricaoPropriedade}")
                .SetFont(fonteNormal)
                .SetMarginBottom(8));

            var tabela = PdfTableBuilder.CriarTabela(
                [4f, 2f],
                ["", "Quantidade"],
                fonteTitulo);

            PdfTableBuilder.AdicionarLinha(tabela, ["Nascimentos", propriedade.TotalNascimentos.ToString(PtBr)], fonteNormal, [TextAlignment.LEFT, TextAlignment.RIGHT]);
            PdfTableBuilder.AdicionarLinha(tabela, ["Compras", propriedade.TotalCompras.ToString(PtBr)], fonteNormal, [TextAlignment.LEFT, TextAlignment.RIGHT]);
            PdfTableBuilder.AdicionarLinha(tabela, ["Vendas", propriedade.TotalVendas.ToString(PtBr)], fonteNormal, [TextAlignment.LEFT, TextAlignment.RIGHT]);
            PdfTableBuilder.AdicionarLinha(tabela, ["Óbitos", propriedade.TotalObitos.ToString(PtBr)], fonteNormal, [TextAlignment.LEFT, TextAlignment.RIGHT]);

            document.Add(tabela);
            document.Add(new Paragraph(" ").SetMarginBottom(4));
        }

        AdicionarResumoConsolidado(document, modelo.Resumo, fonteTitulo, fonteNormal);

        document.Close();
        return stream.ToArray();
    }

    private static void AdicionarCabecalhoPrincipal(Document document, RelatorioRebanhoDto modelo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(modelo.NomeProdutor)
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
            .SetMarginBottom(14));
    }

    private static void AdicionarCabecalhoTecnico(Document document, DateTime dataGeracao, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph($"Data: {dataGeracao:dd/MM/yy}").SetFont(fonteNormal).SetFontSize(9));
        document.Add(new Paragraph($"Hora: {dataGeracao:HH:mm}").SetFont(fonteNormal).SetFontSize(9));
        document.Add(new Paragraph("RELATÓRIO DE MOVIMENTAÇÃO DE REBANHO")
            .SetFont(fonteTitulo)
            .SetFontSize(13)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(8)
            .SetMarginBottom(10));
    }

    private static void AdicionarResumoConsolidado(Document document, ResumoRebanhoAnualDto resumo, PdfFont fonteTitulo, PdfFont fonteNormal)
    {
        document.Add(new Paragraph(" "));
        document.Add(new Paragraph($"Total de Nascimentos: {resumo.TotalNascimentos}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Compras: {resumo.TotalCompras}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Vendas: {resumo.TotalVendas}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Total de Óbitos: {resumo.TotalObitos}").SetFont(fonteNormal));
        document.Add(new Paragraph($"Saldo do Rebanho no Ano: {resumo.SaldoRebanhoAno}")
            .SetFont(fonteTitulo)
            .SetMarginTop(4));
    }

    private static (DateTime dataInicio, DateTime dataFim) ResolverPeriodo(int ano, DateTime? dataInicioCustom = null, DateTime? dataFimCustom = null)
    {
        if (dataInicioCustom.HasValue && dataFimCustom.HasValue)
        {
            return (dataInicioCustom.Value.Date, dataFimCustom.Value.Date);
        }

        var dataInicio = new DateTime(ano, 1, 1);
        var dataFim = new DateTime(ano, 12, 31, 23, 59, 59);
        return (dataInicio, dataFim);
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
}
