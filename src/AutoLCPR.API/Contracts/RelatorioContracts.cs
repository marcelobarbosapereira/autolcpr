using AutoLCPR.Domain.Entities;

namespace AutoLCPR.API.Contracts;

public sealed record RelatorioAnualJsonResponse(
    int AnoFiscal,
    DateTime DataInicio,
    DateTime DataFim,
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Margem,
    IReadOnlyList<ResumoMensalItem> ResumoMensal);

public sealed record ResumoMensalItem(int Mes, decimal Receita, decimal Despesa);

public sealed record RelatorioRebanhoJsonResponse(
    int AnoFiscal,
    int TotalNascimentos,
    int TotalEntradas,
    int TotalSaidas,
    int TotalMortes,
    decimal SaldoInicial,
    decimal SaldoFinal);

public sealed record RelatorioFinanceiroJsonResponse(
    DateTime DataInicio,
    DateTime DataFim,
    TipoLancamento Tipo,
    decimal Total,
    IReadOnlyList<RelatorioFinanceiroItem> Itens);

public sealed record RelatorioFinanceiroItem(DateTime Data, string ClienteFornecedor, string Descricao, decimal Valor);
