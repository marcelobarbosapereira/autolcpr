using System.Text.RegularExpressions;
using AutoLCPR.API.Contracts;
using AutoLCPR.API.Extensions;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
[Route("api/v1/produtores")]
public class ProdutoresController : ControllerBase
{
    private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);
    private readonly IProdutorRepository _repository;

    public ProdutoresController(IProdutorRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProdutorResponse>>> Listar(CancellationToken cancellationToken)
    {
        var produtores = (await _repository.GetAllAsync()).OrderBy(p => p.Nome).Select(p => p.ToResponse()).ToList();
        return Ok(produtores);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutorResponse>> Obter(int id, CancellationToken cancellationToken)
    {
        var produtor = await _repository.GetByIdAsync(id);
        if (produtor is null)
        {
            return NotFound();
        }

        return Ok(produtor.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<ProdutorResponse>> Criar([FromBody] ProdutorRequest request, CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var nome = request.Nome.Trim();
        var cpf = NormalizarSomenteDigitos(request.Cpf);

        var existente = await _repository.GetByNomeAsync(nome);
        if (existente is not null && string.Equals(existente.Nome, nome, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "Já existe um produtor com esse nome." });
        }

        var novo = new Produtor
        {
            Nome = nome,
            Cpf = cpf,
            InscricaoEstadual = string.IsNullOrWhiteSpace(request.InscricaoEstadual)
                ? $"NAO-INFORMADA-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
                : request.InscricaoEstadual.Trim()
        };

        var id = await _repository.AddAsync(novo);
        var produtor = await _repository.GetByIdAsync(id);
        return CreatedAtAction(nameof(Obter), new { id }, produtor!.ToResponse());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProdutorResponse>> Atualizar(int id, [FromBody] ProdutorRequest request, CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);
        if (erro is not null)
        {
            return BadRequest(new { message = erro });
        }

        var produtor = await _repository.GetByIdAsync(id);
        if (produtor is null)
        {
            return NotFound();
        }

        var nome = request.Nome.Trim();
        var existente = await _repository.GetByNomeAsync(nome);
        if (existente is not null && existente.Id != id && string.Equals(existente.Nome, nome, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "Já existe outro produtor com esse nome." });
        }

        produtor.Nome = nome;
        produtor.Cpf = NormalizarSomenteDigitos(request.Cpf);
        if (!string.IsNullOrWhiteSpace(request.InscricaoEstadual))
        {
            produtor.InscricaoEstadual = request.InscricaoEstadual.Trim();
        }

        await _repository.UpdateAsync(produtor);
        var atualizado = await _repository.GetByIdAsync(id);
        return Ok(atualizado!.ToResponse());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var produtor = await _repository.GetByIdAsync(id);
        if (produtor is null)
        {
            return NotFound();
        }

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    private static string? ValidarRequest(ProdutorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "Nome do produtor é obrigatório.";
        }

        if (NormalizarSomenteDigitos(request.Cpf).Length != 11)
        {
            return "CPF deve ter 11 dígitos.";
        }

        return null;
    }

    private static string NormalizarSomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return ApenasDigitosRegex.Replace(valor, string.Empty);
    }
}
