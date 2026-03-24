using System.Globalization;
using AutoLCPR.Application.Relatorios.Documents;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using QuestPDF.Fluent;

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
            DataGeracao = DateTime.Now,
            ResumoMensal = resumoMensal,
            TotalReceitas = dados.TotalReceitas,
            TotalDespesas = dados.TotalDespesas,
            MargemAnual = dados.TotalReceitas - dados.TotalDespesas,
            ResumoRebanho = resumoRebanho
        };
    }

    private async Task<byte[]> GerarPdfAsync(RelatorioAnualDto modelo, int? produtorId, CancellationToken cancellationToken)
    {
        var receitas = new List<Lancamento>();
        await foreach (var item in _lancamentoRepository.StreamPorTipoAsync(modelo.DataInicio, modelo.DataFim, TipoLancamento.Receita, produtorId, cancellationToken))
        {
            receitas.Add(item);
        }

        var despesas = new List<Lancamento>();
        await foreach (var item in _lancamentoRepository.StreamPorTipoAsync(modelo.DataInicio, modelo.DataFim, TipoLancamento.Despesa, produtorId, cancellationToken))
        {
            despesas.Add(item);
        }

        var document = new RelatorioAnualDocument(modelo, receitas, despesas);
        return document.GeneratePdf();
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
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontFamily(QuestPDF.Helpers.Fonts.Arial).FontSize(10));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().AlignCenter().Text("LIVRO CAIXA - RELATÓRIO ANUAL").SemiBold().FontSize(18);
                    column.Item().Text($"Produtor: {SanitizarTexto(modelo.NomeProdutor)}");
                    column.Item().Text($"Ano Base: {modelo.AnoBase}");
                    column.Item().Text($"Exercício: {modelo.AnoExercicio}");
                    column.Item().Text($"Receita Total: {string.Format(PtBr, "{0:C}", modelo.TotalReceitas)}");
                    column.Item().Text($"Despesa Total: {string.Format(PtBr, "{0:C}", modelo.TotalDespesas)}");
                    column.Item().Text($"Margem: {string.Format(PtBr, "{0:C}", modelo.MargemAnual)}");
                    column.Item().PaddingTop(10).Text("Obs: O layout completo não pôde ser renderizado e foi gerada uma versão simplificada.").FontSize(9);
                    column.Item().Text($"Detalhe técnico: {SanitizarTexto(ex.Message)}").FontSize(8);
                });
            });
        }).GeneratePdf();
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
