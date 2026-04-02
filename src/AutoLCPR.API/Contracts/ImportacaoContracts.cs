using AutoLCPR.Application.DTOs;

namespace AutoLCPR.API.Contracts;

public sealed record NfeProcessarRequest(int ProdutorId, bool Reprocessar = false);

public sealed record NfeUploadResponse(int ProdutorId, string DiretorioDestino, int ArquivosRecebidos, int ArquivosSalvos, IReadOnlyList<string> ArquivosIgnorados);

public sealed record NfeProcessarResponse(
    int ProdutorId,
    int ArquivosHtml,
    int NotasProcessadas,
    int NotasImportadas,
    int NotasJaExistiam,
    int NotasIgnoradasPorRegra,
    int LancamentosCriados,
    IReadOnlyList<string> Mensagens);

public sealed record ExtratoPreviewResponse(
    string NomeArquivo,
    bool ParserImplementado,
    string Observacao,
    ExtratoRebanhoPdfDTO Resultado);

public sealed record ExtratoProcessarResponse(
    int ProdutorId,
    string Inscricao,
    bool RebanhoAtualizado,
    bool RebanhoCriado,
    string Mensagem,
    ExtratoRebanhoPdfDTO Resultado);
