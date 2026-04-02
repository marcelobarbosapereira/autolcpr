namespace AutoLCPR.API.Contracts;

public sealed record NfeImportConfigRequest(
    string PastaHtml,
    string? ImagemCabecalho,
    IReadOnlyList<string> IgnorarCFOP,
    IReadOnlyList<string> IgnorarNatureza,
    IReadOnlyList<string> CFOPReceita,
    IReadOnlyList<string> CFOPDespesa,
    IReadOnlyList<string> NaturezaReceita,
    IReadOnlyList<string> NaturezaDespesa);

public sealed record NfeImportConfigResponse(
    string PastaHtml,
    string? ImagemCabecalho,
    IReadOnlyList<string> IgnorarCFOP,
    IReadOnlyList<string> IgnorarNatureza,
    IReadOnlyList<string> CFOPReceita,
    IReadOnlyList<string> CFOPDespesa,
    IReadOnlyList<string> NaturezaReceita,
    IReadOnlyList<string> NaturezaDespesa);
