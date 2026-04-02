using AutoLCPR.API.Contracts;
using AutoLCPR.Domain.Entities;

namespace AutoLCPR.API.Extensions;

public static class MappingExtensions
{
    public static ProdutorResponse ToResponse(this Produtor produtor)
        => new(produtor.Id, produtor.Nome, produtor.Cpf, produtor.InscricaoEstadual, produtor.CreatedAt, produtor.UpdatedAt);

    public static NotaFiscalResponse ToResponse(this NotaFiscal nota)
        => new(
            nota.Id,
            nota.ProdutorId,
            nota.ChaveAcesso,
            nota.DataEmissao,
            nota.NumeroDaNota,
            nota.ValorNotaFiscal,
            nota.Origem,
            nota.Destino,
            nota.Descricao,
            nota.TipoNota,
            nota.NaturezaOperacao,
            nota.Cfops,
            nota.ItensDescricao,
            nota.CreatedAt,
            nota.UpdatedAt);

    public static RebanhoResponse ToResponse(this Rebanho rebanho)
        => new(
            rebanho.Id,
            rebanho.ProdutorId,
            rebanho.IdRebanho,
            rebanho.NomeRebanho,
            rebanho.Mortes,
            rebanho.Nascimentos,
            rebanho.Entradas,
            rebanho.Saidas,
            rebanho.SaldoInicial,
            rebanho.SaldoFinal,
            rebanho.CreatedAt,
            rebanho.UpdatedAt);

    public static NfeImportConfigResponse ToResponse(this NfeImportConfig config)
        => new(
            config.PastaHtml,
            config.ImagemCabecalho,
            config.IgnorarCFOP,
            config.IgnorarNatureza,
            config.CFOPReceita,
            config.CFOPDespesa,
            config.NaturezaReceita,
            config.NaturezaDespesa);
}
