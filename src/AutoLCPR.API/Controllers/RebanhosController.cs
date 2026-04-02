using AutoLCPR.API.Contracts;
using AutoLCPR.API.Extensions;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
public class RebanhosController : ControllerBase
{
    private readonly IRebanhoRepository _rebanhoRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RebanhosController(IRebanhoRepository rebanhoRepository, IProdutorRepository produtorRepository)
    {
        _rebanhoRepository = rebanhoRepository;
        _produtorRepository = produtorRepository;
    }

    [HttpGet("api/v1/produtores/{produtorId:int}/rebanhos")]
    public async Task<ActionResult<IReadOnlyList<RebanhoResponse>>> ListarPorProdutor(int produtorId, [FromQuery] string? busca, CancellationToken cancellationToken)
    {
        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return NotFound(new { message = "Produtor não encontrado." });
        }

        var rebanhos = (await _rebanhoRepository.GetByProdutorIdAsync(produtorId)).ToList();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            rebanhos = rebanhos.Where(item =>
                    Contem(item.IdRebanho, termo)
                    || Contem(item.NomeRebanho, termo)
                    || Contem(item.Nascimentos.ToString(), termo)
                    || Contem(item.Entradas.ToString(), termo)
                    || Contem(item.Saidas.ToString(), termo)
                    || Contem(item.Mortes.ToString(), termo))
                .ToList();
        }

        return Ok(rebanhos.OrderBy(item => item.NomeRebanho).Select(item => item.ToResponse()).ToList());
    }

    [HttpGet("api/v1/rebanhos/{id:int}")]
    public async Task<ActionResult<RebanhoResponse>> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var rebanho = await _rebanhoRepository.GetByIdAsync(id);
        if (rebanho is null)
        {
            return NotFound();
        }

        return Ok(rebanho.ToResponse());
    }

    [HttpPost("api/v1/produtores/{produtorId:int}/rebanhos")]
    public async Task<ActionResult<RebanhoResponse>> Criar(int produtorId, [FromBody] RebanhoRequest request, CancellationToken cancellationToken)
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

        var erro = Validar(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var entidade = ToEntity(request);
        var id = await _rebanhoRepository.AddAsync(entidade);
        var criado = await _rebanhoRepository.GetByIdAsync(id);
        return CreatedAtAction(nameof(ObterPorId), new { id }, criado!.ToResponse());
    }

    [HttpPut("api/v1/rebanhos/{id:int}")]
    public async Task<ActionResult<RebanhoResponse>> Atualizar(int id, [FromBody] RebanhoRequest request, CancellationToken cancellationToken)
    {
        var erro = Validar(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var rebanho = await _rebanhoRepository.GetByIdAsync(id);
        if (rebanho is null)
        {
            return NotFound();
        }

        rebanho.ProdutorId = request.ProdutorId;
        rebanho.IdRebanho = request.IdRebanho.Trim();
        rebanho.NomeRebanho = request.NomeRebanho.Trim();
        rebanho.Mortes = request.Mortes;
        rebanho.Nascimentos = request.Nascimentos;
        rebanho.Entradas = request.Entradas;
        rebanho.Saidas = request.Saidas;
        rebanho.SaldoInicial = request.SaldoInicial;
        rebanho.SaldoFinal = request.SaldoFinal;

        await _rebanhoRepository.UpdateAsync(rebanho);
        var atualizado = await _rebanhoRepository.GetByIdAsync(id);
        return Ok(atualizado!.ToResponse());
    }

    [HttpDelete("api/v1/rebanhos/{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var rebanho = await _rebanhoRepository.GetByIdAsync(id);
        if (rebanho is null)
        {
            return NotFound();
        }

        await _rebanhoRepository.DeleteAsync(id);
        return NoContent();
    }

    private static Rebanho ToEntity(RebanhoRequest request)
    {
        return new Rebanho
        {
            ProdutorId = request.ProdutorId,
            IdRebanho = request.IdRebanho.Trim(),
            NomeRebanho = request.NomeRebanho.Trim(),
            Mortes = request.Mortes,
            Nascimentos = request.Nascimentos,
            Entradas = request.Entradas,
            Saidas = request.Saidas,
            SaldoInicial = request.SaldoInicial,
            SaldoFinal = request.SaldoFinal
        };
    }

    private static string? Validar(RebanhoRequest request)
    {
        if (request.ProdutorId <= 0)
        {
            return "Produtor inválido.";
        }

        if (string.IsNullOrWhiteSpace(request.IdRebanho))
        {
            return "ID do rebanho é obrigatório.";
        }

        if (string.IsNullOrWhiteSpace(request.NomeRebanho))
        {
            return "Nome do rebanho é obrigatório.";
        }

        if (request.Mortes < 0 || request.Nascimentos < 0 || request.Entradas < 0 || request.Saidas < 0)
        {
            return "Valores de mortes, nascimentos, entradas e saídas não podem ser negativos.";
        }

        return null;
    }

    private static bool Contem(string? source, string termo)
    {
        return !string.IsNullOrWhiteSpace(source) && source.Contains(termo, StringComparison.OrdinalIgnoreCase);
    }
}
