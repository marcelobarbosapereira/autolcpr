namespace AutoLCPR.API.Contracts;

public sealed record RebanhoRequest(
    int ProdutorId,
    string IdRebanho,
    string NomeRebanho,
    int Mortes,
    int Nascimentos,
    int Entradas,
    int Saidas,
    decimal SaldoInicial,
    decimal SaldoFinal);

public sealed record RebanhoResponse(
    int Id,
    int ProdutorId,
    string IdRebanho,
    string NomeRebanho,
    int Mortes,
    int Nascimentos,
    int Entradas,
    int Saidas,
    decimal SaldoInicial,
    decimal SaldoFinal,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
