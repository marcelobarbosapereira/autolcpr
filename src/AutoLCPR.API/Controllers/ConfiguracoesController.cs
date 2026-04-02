using AutoLCPR.API.Contracts;
using AutoLCPR.API.Extensions;
using AutoLCPR.Application.Services;
using AutoLCPR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
[Route("api/v1/configuracoes")]
public class ConfiguracoesController : ControllerBase
{
    private readonly NfeConfigService _configService;

    public ConfiguracoesController(NfeConfigService configService)
    {
        _configService = configService;
    }

    [HttpGet("importacao-nfe")]
    public async Task<ActionResult<NfeImportConfigResponse>> Obter(CancellationToken cancellationToken)
    {
        var config = await _configService.CarregarConfiguracaoAsync();
        return Ok(config.ToResponse());
    }

    [HttpPut("importacao-nfe")]
    public async Task<ActionResult<NfeImportConfigResponse>> Atualizar([FromBody] NfeImportConfigRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PastaHtml))
        {
            return BadRequest(new { message = "PastaHtml é obrigatório." });
        }

        var config = new NfeImportConfig
        {
            PastaHtml = request.PastaHtml,
            ImagemCabecalho = request.ImagemCabecalho,
            IgnorarCFOP = request.IgnorarCFOP.ToList(),
            IgnorarNatureza = request.IgnorarNatureza.ToList(),
            CFOPReceita = request.CFOPReceita.ToList(),
            CFOPDespesa = request.CFOPDespesa.ToList(),
            NaturezaReceita = request.NaturezaReceita.ToList(),
            NaturezaDespesa = request.NaturezaDespesa.ToList()
        };

        await _configService.SalvarConfiguracaoAsync(config);
        _configService.LimparCache();

        var atualizado = await _configService.CarregarConfiguracaoAsync();
        return Ok(atualizado.ToResponse());
    }

    [HttpPost("cabecalho")]
    public async Task<ActionResult<object>> UploadCabecalho([FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { message = "Arquivo não informado." });
        }

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var permitidas = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
        if (!permitidas.Contains(extensao))
        {
            return BadRequest(new { message = "Formato inválido para imagem de cabeçalho." });
        }

        var diretorio = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(diretorio);

        foreach (var ext in permitidas)
        {
            var anterior = Path.Combine(diretorio, $"cabecalho_relatorio{ext}");
            if (System.IO.File.Exists(anterior))
            {
                System.IO.File.Delete(anterior);
            }
        }

        var destino = Path.Combine(diretorio, $"cabecalho_relatorio{extensao}");
        await using (var fs = System.IO.File.Create(destino))
        {
            await arquivo.CopyToAsync(fs, cancellationToken);
        }

        var config = await _configService.CarregarConfiguracaoAsync();
        config.ImagemCabecalho = destino;
        await _configService.SalvarConfiguracaoAsync(config);
        _configService.LimparCache();

        return Ok(new { caminho = destino });
    }
}
