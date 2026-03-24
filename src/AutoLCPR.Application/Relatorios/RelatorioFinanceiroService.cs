using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Application.Relatorios.Documents;
using QuestPDF.Fluent;

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

        var lancamentos = new List<Lancamento>();
        await foreach (var item in _lancamentoRepository.StreamFinanceiroAsync(inicio, fim, tipo, produtor?.Id, null, cancellationToken))
        {
            lancamentos.Add(item);
        }

        var document = new RelatorioFinanceiroDocument(modelo, lancamentos);
        return document.GeneratePdf();
    }

    private async Task<decimal> CalcularTotalAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId, CancellationToken cancellationToken)
    {
        return await _lancamentoRepository.ObterTotalFinanceiroAsync(dataInicial, dataFinal, tipo, produtorId, null, cancellationToken);
    }

    private static string FormatarMoeda(decimal valor)
    {
        return string.Format(PtBr, "{0:C}", valor);
    }
}
