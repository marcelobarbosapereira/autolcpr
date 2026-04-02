using AutoLCPR.API.Contracts;
using AutoLCPR.API.Extensions;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
public class NotasController : ControllerBase
{
    private readonly INotaFiscalRepository _notaRepository;
    private readonly IProdutorRepository _produtorRepository;

    public NotasController(INotaFiscalRepository notaRepository, IProdutorRepository produtorRepository)
    {
        _notaRepository = notaRepository;
        _produtorRepository = produtorRepository;
    }

    [HttpGet("api/v1/produtores/{produtorId:int}/notas")]
    public async Task<ActionResult<IReadOnlyList<NotaFiscalResponse>>> ListarPorProdutor(
        int produtorId,
        [FromQuery] string? tipo,
        [FromQuery] string? busca,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return NotFound(new { message = "Produtor não encontrado." });
        }

        var notas = (await _notaRepository.GetByProdutorIdAsync(produtorId)).ToList();

        if (TryParseTipo(tipo, out var tipoNota) && tipoNota is { } tipoNotaValor)
        {
            notas = notas.Where(item => item.TipoNota == tipoNotaValor).ToList();
        }

        if (dataInicio.HasValue)
        {
            notas = notas.Where(item => item.DataEmissao.Date >= dataInicio.Value.Date).ToList();
        }

        if (dataFim.HasValue)
        {
            notas = notas.Where(item => item.DataEmissao.Date <= dataFim.Value.Date).ToList();
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            notas = notas.Where(item =>
                    Contem(item.NumeroDaNota, termo)
                    || Contem(item.Origem, termo)
                    || Contem(item.Destino, termo)
                    || Contem(item.Descricao, termo)
                    || Contem(item.DataEmissao.ToString("dd/MM/yyyy"), termo)
                    || Contem(item.ValorNotaFiscal.ToString("N2"), termo))
                .ToList();
        }

        return Ok(notas.OrderByDescending(item => item.DataEmissao).Select(item => item.ToResponse()).ToList());
    }

    [HttpPost("api/v1/produtores/{produtorId:int}/notas")]
    public async Task<ActionResult<NotaFiscalResponse>> Criar(int produtorId, [FromBody] NotaFiscalRequest request, CancellationToken cancellationToken)
    {
        if (request.ProdutorId != produtorId)
        {
            return BadRequest(new { message = "Produtor do payload difere da rota." });
        }

        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return NotFound(new { message = "Produtor não encontrado." });
        }

        var erro = ValidarNota(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var nota = ToEntity(request);
        var id = await _notaRepository.AddAsync(nota);
        var criada = await _notaRepository.GetByIdAsync(id);
        return CreatedAtAction(nameof(ObterPorId), new { id }, criada!.ToResponse());
    }

    [HttpGet("api/v1/notas/{id:int}")]
    public async Task<ActionResult<NotaFiscalResponse>> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var nota = await _notaRepository.GetByIdAsync(id);
        if (nota is null)
        {
            return NotFound();
        }

        return Ok(nota.ToResponse());
    }

    [HttpPut("api/v1/notas/{id:int}")]
    public async Task<ActionResult<NotaFiscalResponse>> Atualizar(int id, [FromBody] NotaFiscalRequest request, CancellationToken cancellationToken)
    {
        var erro = ValidarNota(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var nota = await _notaRepository.GetByIdAsync(id);
        if (nota is null)
        {
            return NotFound();
        }

        nota.ProdutorId = request.ProdutorId;
        nota.ChaveAcesso = request.ChaveAcesso;
        nota.DataEmissao = request.DataEmissao;
        nota.NumeroDaNota = request.NumeroDaNota.Trim();
        nota.ValorNotaFiscal = request.ValorNotaFiscal;
        nota.Origem = request.Origem.Trim();
        nota.Destino = request.Destino.Trim();
        nota.Descricao = request.Descricao.Trim();
        nota.TipoNota = request.TipoNota;
        nota.NaturezaOperacao = request.NaturezaOperacao?.Trim();
        nota.Cfops = request.Cfops?.Trim();
        nota.ItensDescricao = request.ItensDescricao?.Trim();

        await _notaRepository.UpdateAsync(nota);
        var atualizada = await _notaRepository.GetByIdAsync(id);
        return Ok(atualizada!.ToResponse());
    }

    [HttpDelete("api/v1/notas/{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var nota = await _notaRepository.GetByIdAsync(id);
        if (nota is null)
        {
            return NotFound();
        }

        await _notaRepository.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("api/v1/notas/exclusao-lote")]
    public async Task<IActionResult> ExcluirLote([FromBody] NotaFiscalBatchDeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids is null || request.Ids.Count == 0)
        {
            return BadRequest(new { message = "Informe ao menos um id para exclusão." });
        }

        var removidos = 0;
        foreach (var id in request.Ids.Distinct())
        {
            var nota = await _notaRepository.GetByIdAsync(id);
            if (nota is null)
            {
                continue;
            }

            await _notaRepository.DeleteAsync(id);
            removidos++;
        }

        return Ok(new { removidos });
    }

    private static NotaFiscal ToEntity(NotaFiscalRequest request)
    {
        return new NotaFiscal
        {
            ProdutorId = request.ProdutorId,
            ChaveAcesso = request.ChaveAcesso?.Trim(),
            DataEmissao = request.DataEmissao,
            NumeroDaNota = request.NumeroDaNota.Trim(),
            ValorNotaFiscal = request.ValorNotaFiscal,
            Origem = request.Origem.Trim(),
            Destino = request.Destino.Trim(),
            Descricao = request.Descricao.Trim(),
            TipoNota = request.TipoNota,
            NaturezaOperacao = request.NaturezaOperacao?.Trim(),
            Cfops = request.Cfops?.Trim(),
            ItensDescricao = request.ItensDescricao?.Trim()
        };
    }

    private static string? ValidarNota(NotaFiscalRequest request)
    {
        if (request.ProdutorId <= 0)
        {
            return "Produtor inválido.";
        }

        if (string.IsNullOrWhiteSpace(request.NumeroDaNota))
        {
            return "Número da nota é obrigatório.";
        }

        if (request.ValorNotaFiscal <= 0)
        {
            return "Valor da nota deve ser maior que zero.";
        }

        if (request.TipoNota == TipoNota.Entrada && string.IsNullOrWhiteSpace(request.Origem))
        {
            return "Origem é obrigatória para nota de entrada.";
        }

        if (request.TipoNota == TipoNota.Saida && string.IsNullOrWhiteSpace(request.Destino))
        {
            return "Destino é obrigatório para nota de saída.";
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            return "Descrição é obrigatória.";
        }

        return null;
    }

    private static bool TryParseTipo(string? tipo, out TipoNota? tipoNota)
    {
        tipoNota = null;
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return false;
        }

        tipo = tipo.Trim().ToLowerInvariant();
        tipoNota = tipo switch
        {
            "entrada" => TipoNota.Entrada,
            "saida" => TipoNota.Saida,
            _ => null
        };

        return tipoNota.HasValue;
    }

    private static bool Contem(string? source, string termo)
    {
        return !string.IsNullOrWhiteSpace(source) && source.Contains(termo, StringComparison.OrdinalIgnoreCase);
    }
}
