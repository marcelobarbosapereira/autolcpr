using AutoLCPR.Domain.Repositories;
using AutoLCPR.Application.Relatorios.Documents;
using QuestPDF.Fluent;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioRebanhoService : IRelatorioRebanhoService
{
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
                TotalObitos = item.Mortes,
                SaldoInicial = item.SaldoInicial,
                SaldoFinal = item.SaldoFinal
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
                TotalObitos = 0,
                SaldoInicial = 0,
                SaldoFinal = 0
            });
        }

        var resumoConsolidado = new ResumoRebanhoAnualDto
        {
            TotalNascimentos = propriedades.Sum(item => item.TotalNascimentos),
            TotalCompras = propriedades.Sum(item => item.TotalCompras),
            TotalVendas = propriedades.Sum(item => item.TotalVendas),
            TotalObitos = propriedades.Sum(item => item.TotalObitos),
            SaldoRebanhoAno = propriedades.Sum(item => item.TotalNascimentos + item.TotalCompras - item.TotalVendas - item.TotalObitos),
            TotalSaldoInicial = propriedades.Sum(item => item.SaldoInicial),
            TotalSaldoFinal = propriedades.Sum(item => item.SaldoFinal)
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
                SaldoRebanhoAno = resumoConsolidado.SaldoRebanhoAno,
                TotalSaldoInicial = resumoConsolidado.TotalSaldoInicial,
                TotalSaldoFinal = resumoConsolidado.TotalSaldoFinal
            }
        };

        var document = new RelatorioRebanhoDocument(modelo);
        return document.GeneratePdf();
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
}
