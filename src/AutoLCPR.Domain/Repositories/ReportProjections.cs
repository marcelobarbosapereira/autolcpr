namespace AutoLCPR.Domain.Repositories;

public sealed class ResumoMensalFinanceiro
{
    public int Mes { get; init; }
    public decimal Receita { get; init; }
    public decimal Despesa { get; init; }
}

public sealed class ResumoMovimentacaoRebanho
{
    public required string TipoMovimentacao { get; init; }
    public int Quantidade { get; init; }
}

public sealed class ResumoConsolidadoRebanho
{
    public int TotalNascimentos { get; init; }
    public int TotalCompras { get; init; }
    public int TotalVendas { get; init; }
    public int TotalObitos { get; init; }
    public int SaldoRebanhoAno => (TotalNascimentos + TotalCompras) - (TotalVendas + TotalObitos);
}
