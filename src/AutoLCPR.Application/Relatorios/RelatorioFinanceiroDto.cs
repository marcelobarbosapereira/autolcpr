using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioFinanceiroDto
{
    public required string NomeProdutor { get; init; }
    public required DateTime DataInicial { get; init; }
    public required DateTime DataFinal { get; init; }
    public required DateTime DataGeracao { get; init; }
    public TipoLancamento Tipo { get; init; }
    public decimal Total { get; init; }
}
