using AutoLCPR.API.Contracts;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
[Route("api/v1/produtores/{produtorId:int}/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IProdutorRepository _produtorRepository;
    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly IRebanhoRepository _rebanhoRepository;

    public DashboardController(IProdutorRepository produtorRepository, INotaFiscalRepository notaFiscalRepository, IRebanhoRepository rebanhoRepository)
    {
        _produtorRepository = produtorRepository;
        _notaFiscalRepository = notaFiscalRepository;
        _rebanhoRepository = rebanhoRepository;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Obter(int produtorId, [FromQuery] string? mes, CancellationToken cancellationToken)
    {
        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return NotFound(new { message = "Produtor não encontrado." });
        }

        var notas = (await _notaFiscalRepository.GetByProdutorIdAsync(produtorId)).ToList();
        var rebanhos = (await _rebanhoRepository.GetByProdutorIdAsync(produtorId)).ToList();

        var referencia = ParseMes(mes) ?? DateTime.Now;

        var receitas = notas.Where(item => item.TipoNota == TipoNota.Saida).OrderByDescending(item => item.DataEmissao).ToList();
        var despesas = notas.Where(item => item.TipoNota == TipoNota.Entrada).OrderByDescending(item => item.DataEmissao).ToList();

        var receitasMes = receitas
            .Where(item => item.DataEmissao.Year == referencia.Year && item.DataEmissao.Month == referencia.Month)
            .Sum(item => item.ValorNotaFiscal);

        var despesasMes = despesas
            .Where(item => item.DataEmissao.Year == referencia.Year && item.DataEmissao.Month == referencia.Month)
            .Sum(item => item.ValorNotaFiscal);

        var notasMesCount = notas.Count(item => item.DataEmissao.Year == referencia.Year && item.DataEmissao.Month == referencia.Month);

        var response = new DashboardResponse(
            produtorId,
            rebanhos.Count,
            notasMesCount,
            receitasMes,
            despesasMes,
            receitas.Sum(item => item.ValorNotaFiscal) - despesas.Sum(item => item.ValorNotaFiscal),
            receitas.Select(item => new DashboardNotaResumo(item.Id, item.NumeroDaNota, item.DataEmissao, item.ValorNotaFiscal, item.TipoNota, item.Origem, item.Destino, item.Descricao)).ToList(),
            despesas.Select(item => new DashboardNotaResumo(item.Id, item.NumeroDaNota, item.DataEmissao, item.ValorNotaFiscal, item.TipoNota, item.Origem, item.Destino, item.Descricao)).ToList(),
            rebanhos.Select(item => new DashboardRebanhoResumo(item.Id, item.IdRebanho, item.NomeRebanho, item.Mortes, item.Nascimentos, item.Entradas, item.Saidas, item.SaldoInicial, item.SaldoFinal)).ToList());

        return Ok(response);
    }

    private static DateTime? ParseMes(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (DateTime.TryParse($"{valor}-01", out var date))
        {
            return date;
        }

        return null;
    }
}
