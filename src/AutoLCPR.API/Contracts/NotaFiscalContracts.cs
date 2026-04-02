using AutoLCPR.Domain.Entities;

namespace AutoLCPR.API.Contracts;

public sealed record NotaFiscalRequest(
    int ProdutorId,
    string? ChaveAcesso,
    DateTime DataEmissao,
    string NumeroDaNota,
    decimal ValorNotaFiscal,
    string Origem,
    string Destino,
    string Descricao,
    TipoNota TipoNota,
    string? NaturezaOperacao,
    string? Cfops,
    string? ItensDescricao);

public sealed record NotaFiscalResponse(
    int Id,
    int ProdutorId,
    string? ChaveAcesso,
    DateTime DataEmissao,
    string NumeroDaNota,
    decimal ValorNotaFiscal,
    string Origem,
    string Destino,
    string Descricao,
    TipoNota TipoNota,
    string? NaturezaOperacao,
    string? Cfops,
    string? ItensDescricao,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record NotaFiscalBatchDeleteRequest(IReadOnlyList<int> Ids);
