using AutoLCPR.Domain.Entities;

namespace AutoLCPR.API.Contracts;

public sealed record DashboardResponse(
    int ProdutorId,
    int TotalRebanhos,
    int NotasFiscaisMes,
    decimal ReceitasMes,
    decimal DespesasMes,
    decimal SaldoFinanceiro,
    IReadOnlyList<DashboardNotaResumo> Receitas,
    IReadOnlyList<DashboardNotaResumo> Despesas,
    IReadOnlyList<DashboardRebanhoResumo> Rebanhos);

public sealed record DashboardNotaResumo(int Id, string NumeroDaNota, DateTime DataEmissao, decimal ValorNotaFiscal, TipoNota TipoNota, string Origem, string Destino, string Descricao);

public sealed record DashboardRebanhoResumo(int Id, string IdRebanho, string NomeRebanho, int Mortes, int Nascimentos, int Entradas, int Saidas, decimal SaldoInicial, decimal SaldoFinal);
