using AutoLCPR.API.Contracts;
using AutoLCPR.Application.Relatorios;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoLCPR.API.Controllers;

[ApiController]
[Route("api/v1/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioAnualService _relatorioAnualService;
    private readonly IRelatorioRebanhoService _relatorioRebanhoService;
    private readonly IRelatorioFinanceiroService _relatorioFinanceiroService;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IRebanhoRepository _rebanhoRepository;
    private readonly IProdutorRepository _produtorRepository;

    public RelatoriosController(
        IRelatorioAnualService relatorioAnualService,
        IRelatorioRebanhoService relatorioRebanhoService,
        IRelatorioFinanceiroService relatorioFinanceiroService,
        ILancamentoRepository lancamentoRepository,
        IRebanhoRepository rebanhoRepository,
        IProdutorRepository produtorRepository)
    {
        _relatorioAnualService = relatorioAnualService;
        _relatorioRebanhoService = relatorioRebanhoService;
        _relatorioFinanceiroService = relatorioFinanceiroService;
        _lancamentoRepository = lancamentoRepository;
        _rebanhoRepository = rebanhoRepository;
        _produtorRepository = produtorRepository;
    }

    [HttpGet("anual")]
    public async Task<IActionResult> Anual([FromQuery] int anoFiscal, [FromQuery] string formato = "json", CancellationToken cancellationToken = default)
    {
        if (anoFiscal < 1900 || anoFiscal > 3000)
        {
            return BadRequest(new { message = "Ano fiscal inválido." });
        }

        if (formato.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _relatorioAnualService.GerarRelatorioAnual(anoFiscal);
            var fileName = $"LivroCaixa_{anoFiscal}_{anoFiscal + 1}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        var dataInicio = new DateTime(anoFiscal, 1, 1);
        var dataFim = new DateTime(anoFiscal, 12, 31, 23, 59, 59);
        var produtor = (await _produtorRepository.GetAllAsync()).OrderBy(item => item.Nome).FirstOrDefault();
        var resumo = await _lancamentoRepository.ObterResumoMensalAsync(dataInicio, dataFim, produtor?.Id, cancellationToken);
        var totalReceitas = await _lancamentoRepository.ObterTotalPorTipoAsync(dataInicio, dataFim, TipoLancamento.Receita, produtor?.Id, cancellationToken);
        var totalDespesas = await _lancamentoRepository.ObterTotalPorTipoAsync(dataInicio, dataFim, TipoLancamento.Despesa, produtor?.Id, cancellationToken);

        var response = new RelatorioAnualJsonResponse(
            anoFiscal,
            dataInicio,
            dataFim,
            totalReceitas,
            totalDespesas,
            totalReceitas - totalDespesas,
            resumo.Select(item => new ResumoMensalItem(item.Mes, item.Receita, item.Despesa)).ToList());

        return Ok(response);
    }

    [HttpGet("rebanho")]
    public async Task<IActionResult> Rebanho([FromQuery] int anoFiscal, [FromQuery] string formato = "json", CancellationToken cancellationToken = default)
    {
        if (anoFiscal < 1900 || anoFiscal > 3000)
        {
            return BadRequest(new { message = "Ano fiscal inválido." });
        }

        if (formato.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _relatorioRebanhoService.GerarRelatorioRebanho(anoFiscal);
            var fileName = $"Rebanho_{anoFiscal}_{anoFiscal + 1}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        var produtor = (await _produtorRepository.GetAllAsync()).OrderBy(item => item.Nome).FirstOrDefault();
        var rebanhos = produtor?.Id is int produtorId
            ? (await _rebanhoRepository.GetByProdutorIdAsync(produtorId)).ToList()
            : (await _rebanhoRepository.GetAllAsync()).ToList();

        var response = new RelatorioRebanhoJsonResponse(
            anoFiscal,
            rebanhos.Sum(item => item.Nascimentos),
            rebanhos.Sum(item => item.Entradas),
            rebanhos.Sum(item => item.Saidas),
            rebanhos.Sum(item => item.Mortes),
            rebanhos.Sum(item => item.SaldoInicial),
            rebanhos.Sum(item => item.SaldoFinal));

        return Ok(response);
    }

    [HttpGet("financeiro")]
    public async Task<IActionResult> Financeiro(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim,
        [FromQuery] TipoLancamento tipo,
        [FromQuery] string formato = "json",
        CancellationToken cancellationToken = default)
    {
        if (dataInicio.Date > dataFim.Date)
        {
            return BadRequest(new { message = "Data inicial não pode ser maior que data final." });
        }

        if (formato.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _relatorioFinanceiroService.GerarRelatorioFinanceiro(dataInicio.Date, dataFim.Date, tipo);
            var fileName = $"{tipo}_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        var produtor = (await _produtorRepository.GetAllAsync()).OrderBy(item => item.Nome).FirstOrDefault();
        var inicio = dataInicio.Date;
        var fim = dataFim.Date.AddDays(1).AddTicks(-1);
        var total = await _lancamentoRepository.ObterTotalFinanceiroAsync(inicio, fim, tipo, produtor?.Id, null, cancellationToken);

        var itens = new List<RelatorioFinanceiroItem>();
        await foreach (var item in _lancamentoRepository.StreamFinanceiroAsync(inicio, fim, tipo, produtor?.Id, null, cancellationToken))
        {
            itens.Add(new RelatorioFinanceiroItem(item.Data, item.ClienteFornecedor, item.Descricao, item.Valor));
        }

        var response = new RelatorioFinanceiroJsonResponse(inicio, dataFim.Date, tipo, total, itens);
        return Ok(response);
    }
}
