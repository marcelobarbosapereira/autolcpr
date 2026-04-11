using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Application.Relatorios.Documents;
using QuestPDF.Fluent;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioFinanceiroService : IRelatorioFinanceiroService
{
    private static readonly System.Globalization.CultureInfo PtBr = new("pt-BR");

    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RelatorioFinanceiroService(INotaFiscalRepository notaFiscalRepository, IProdutorRepository produtorRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
        _produtorRepository = produtorRepository;
    }

    public byte[] GerarRelatorioFinanceiro(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId = null)
    {
        return GerarRelatorioFinanceiroAsync(dataInicial, dataFinal, tipo, produtorId, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task<byte[]> GerarRelatorioFinanceiroAsync(DateTime dataInicial, DateTime dataFinal, TipoLancamento tipo, int? produtorId, CancellationToken cancellationToken)
    {
        var inicio = dataInicial.Date;
        var fim = dataFinal.Date.AddDays(1).AddTicks(-1);

        Produtor? produtor = null;
        if (produtorId.HasValue)
        {
            produtor = await _produtorRepository.GetByIdAsync(produtorId.Value);
        }

        produtor ??= (await _produtorRepository.GetAllAsync())
            .OrderBy(item => item.Nome)
            .FirstOrDefault();

        var tipoNota = tipo == TipoLancamento.Receita ? TipoNota.Saida : TipoNota.Entrada;
        var notas = produtor?.Id is int produtorSelecionadoId
            ? (await _notaFiscalRepository.GetByProdutorIdAsync(produtorSelecionadoId))
                .Where(item => item.TipoNota == tipoNota && item.DataEmissao >= inicio && item.DataEmissao <= fim)
                .OrderByDescending(item => item.DataEmissao)
                .ToList()
            : new List<NotaFiscal>();

        var total = notas.Sum(item => item.ValorNotaFiscal);

        var modelo = new RelatorioFinanceiroDto
        {
            NomeProdutor = produtor?.Nome ?? "PRODUTOR NÃO INFORMADO",
            DataInicial = inicio,
            DataFinal = dataFinal.Date,
            DataGeracao = DateTime.Now,
            Tipo = tipo,
            Total = total
        };

        var lancamentos = notas.Select(item => new Lancamento
        {
            Data = item.DataEmissao,
            Tipo = tipo,
            ClienteFornecedor = item.Destino,
            Descricao = item.Descricao,
            Situacao = "Concluído",
            Valor = item.ValorNotaFiscal,
            Vencimento = item.DataEmissao,
            ProdutorId = item.ProdutorId
        }).ToList();

        var document = new RelatorioFinanceiroDocument(modelo, lancamentos);
        return document.GeneratePdf();
    }

    private static string FormatarMoeda(decimal valor)
    {
        return string.Format(PtBr, "{0:C}", valor);
    }
}
