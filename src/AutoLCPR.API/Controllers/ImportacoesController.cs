using System.Text.RegularExpressions;
using AutoLCPR.API.Contracts;
using AutoLCPR.API.Services;
using AutoLCPR.Application.DTOs;
using AutoLCPR.Application.Services;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
[Route("api/v1/importacoes")]
public class ImportacoesController : ControllerBase
{
    private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);

    private readonly NfeImportOrchestrator _nfeImportOrchestrator;
    private readonly IExtratoRebanhoPdfParserService _extratoParser;
    private readonly IProdutorRepository _produtorRepository;
    private readonly IRebanhoRepository _rebanhoRepository;

    public ImportacoesController(
        NfeImportOrchestrator nfeImportOrchestrator,
        IExtratoRebanhoPdfParserService extratoParser,
        IProdutorRepository produtorRepository,
        IRebanhoRepository rebanhoRepository)
    {
        _nfeImportOrchestrator = nfeImportOrchestrator;
        _extratoParser = extratoParser;
        _produtorRepository = produtorRepository;
        _rebanhoRepository = rebanhoRepository;
    }

    [HttpPost("nfe/upload-html")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<NfeUploadResponse>> UploadHtml([FromForm] int produtorId, [FromForm] List<IFormFile> arquivos, CancellationToken cancellationToken)
    {
        if (produtorId <= 0)
        {
            return BadRequest(new { message = "Produtor inválido." });
        }

        if (arquivos is null || arquivos.Count == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo informado." });
        }

        var (response, error) = await _nfeImportOrchestrator.UploadHtmlAsync(produtorId, arquivos, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(response);
    }

    [HttpPost("nfe/processar")]
    public async Task<ActionResult<NfeProcessarResponse>> ProcessarNfe([FromBody] NfeProcessarRequest request, CancellationToken cancellationToken)
    {
        var (response, error) = await _nfeImportOrchestrator.ProcessarAsync(request, cancellationToken);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(response);
    }

    [HttpPost("extrato/preview")]
    [RequestSizeLimit(30_000_000)]
    public async Task<ActionResult<ExtratoPreviewResponse>> PreviewExtrato([FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { message = "Arquivo não informado." });
        }

        if (!arquivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Formato inválido. Envie um arquivo PDF." });
        }

        var tempPath = await SalvarTemporarioAsync(arquivo, cancellationToken);
        try
        {
            var resultado = await _extratoParser.ParseAsync(tempPath, cancellationToken);
            var response = new ExtratoPreviewResponse(arquivo.FileName, resultado.ParserImplementado, resultado.Observacao, resultado);
            return Ok(response);
        }
        finally
        {
            ApagarTemporario(tempPath);
        }
    }

    [HttpPost("extrato/processar")]
    [RequestSizeLimit(30_000_000)]
    public async Task<ActionResult<ExtratoProcessarResponse>> ProcessarExtrato([FromForm] int produtorId, [FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (produtorId <= 0)
        {
            return BadRequest(new { message = "Produtor inválido." });
        }

        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return NotFound(new { message = "Produtor não encontrado." });
        }

        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { message = "Arquivo não informado." });
        }

        if (!arquivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Formato inválido. Envie um arquivo PDF." });
        }

        var tempPath = await SalvarTemporarioAsync(arquivo, cancellationToken);

        try
        {
            var resultado = await _extratoParser.ParseAsync(tempPath, cancellationToken);
            var inscricaoNormalizada = NormalizarSomenteDigitos(resultado.Inscricao);
            if (inscricaoNormalizada.Length != 9)
            {
                return BadRequest(new { message = "Inscrição inválida ou ausente no extrato." });
            }

            var existente = await _rebanhoRepository.GetByIdRebanhoAsync(inscricaoNormalizada);
            if (existente is not null)
            {
                AtualizarRebanho(existente, resultado, produtorId, inscricaoNormalizada);
                await _rebanhoRepository.UpdateAsync(existente);

                return Ok(new ExtratoProcessarResponse(
                    produtorId,
                    inscricaoNormalizada,
                    RebanhoAtualizado: true,
                    RebanhoCriado: false,
                    Mensagem: "Rebanho existente atualizado com dados do extrato.",
                    Resultado: resultado));
            }

            var novo = CriarRebanho(resultado, produtorId, inscricaoNormalizada);
            await _rebanhoRepository.AddAsync(novo);

            return Ok(new ExtratoProcessarResponse(
                produtorId,
                inscricaoNormalizada,
                RebanhoAtualizado: false,
                RebanhoCriado: true,
                Mensagem: "Novo rebanho criado a partir do extrato.",
                Resultado: resultado));
        }
        finally
        {
            ApagarTemporario(tempPath);
        }
    }

    private static Rebanho CriarRebanho(ExtratoRebanhoPdfDTO resultado, int produtorId, string inscricaoNormalizada)
    {
        return new Rebanho
        {
            IdRebanho = inscricaoNormalizada,
            NomeRebanho = string.IsNullOrWhiteSpace(resultado.NomePropriedade)
                ? $"PROPRIEDADE {inscricaoNormalizada}"
                : resultado.NomePropriedade.Trim(),
            Mortes = resultado.MortesConsumos ?? 0,
            Nascimentos = resultado.Nascimentos ?? 0,
            Entradas = resultado.Entradas ?? 0,
            Saidas = resultado.Saidas ?? 0,
            SaldoInicial = Convert.ToDecimal(resultado.SaldoInicial ?? 0),
            SaldoFinal = Convert.ToDecimal(resultado.SaldoFinal ?? 0),
            ProdutorId = produtorId
        };
    }

    private static void AtualizarRebanho(Rebanho rebanho, ExtratoRebanhoPdfDTO resultado, int produtorId, string inscricaoNormalizada)
    {
        rebanho.IdRebanho = inscricaoNormalizada;
        rebanho.NomeRebanho = string.IsNullOrWhiteSpace(resultado.NomePropriedade)
            ? rebanho.NomeRebanho
            : resultado.NomePropriedade.Trim();
        rebanho.Mortes = resultado.MortesConsumos ?? 0;
        rebanho.Nascimentos = resultado.Nascimentos ?? 0;
        rebanho.Entradas = resultado.Entradas ?? 0;
        rebanho.Saidas = resultado.Saidas ?? 0;
        rebanho.SaldoInicial = Convert.ToDecimal(resultado.SaldoInicial ?? 0);
        rebanho.SaldoFinal = Convert.ToDecimal(resultado.SaldoFinal ?? 0);
        rebanho.ProdutorId = produtorId;
    }

    private static string NormalizarSomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return ApenasDigitosRegex.Replace(valor, string.Empty);
    }

    private static async Task<string> SalvarTemporarioAsync(IFormFile arquivo, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"extrato_{Guid.NewGuid():N}.pdf");
        await using var stream = System.IO.File.Create(tempPath);
        await arquivo.CopyToAsync(stream, cancellationToken);
        return tempPath;
    }

    private static void ApagarTemporario(string caminho)
    {
        if (System.IO.File.Exists(caminho))
        {
            System.IO.File.Delete(caminho);
        }
    }
}
