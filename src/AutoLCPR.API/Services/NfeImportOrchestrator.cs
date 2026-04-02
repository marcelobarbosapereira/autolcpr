using System.Text.RegularExpressions;
using AutoLCPR.API.Contracts;
using AutoLCPR.Application.DTOs;
using AutoLCPR.Application.Services;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoLCPR.API.Services;

public sealed class NfeImportOrchestrator
{
    private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);

    private readonly IProdutorRepository _produtorRepository;
    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly NfeImportService _nfeImportService;
    private readonly AppDbContext _dbContext;

    public NfeImportOrchestrator(
        IProdutorRepository produtorRepository,
        INotaFiscalRepository notaFiscalRepository,
        ILancamentoRepository lancamentoRepository,
        NfeImportService nfeImportService,
        AppDbContext dbContext)
    {
        _produtorRepository = produtorRepository;
        _notaFiscalRepository = notaFiscalRepository;
        _lancamentoRepository = lancamentoRepository;
        _nfeImportService = nfeImportService;
        _dbContext = dbContext;
    }

    public async Task<(NfeUploadResponse? response, string? error)> UploadHtmlAsync(int produtorId, IReadOnlyList<IFormFile> arquivos, CancellationToken cancellationToken)
    {
        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor is null)
        {
            return (null, "Produtor não encontrado.");
        }

        var cpfProdutor = NormalizarSomenteDigitos(produtor.Cpf);
        if (cpfProdutor.Length != 11)
        {
            return (null, "CPF do produtor inválido. Atualize o cadastro antes de importar.");
        }

        var diretorio = await _nfeImportService.CriarPastaProdutorAsync(cpfProdutor);
        var ignorados = new List<string>();
        var salvos = 0;

        foreach (var arquivo in arquivos)
        {
            if (arquivo.Length <= 0)
            {
                ignorados.Add($"{arquivo.FileName}: vazio");
                continue;
            }

            if (!arquivo.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                ignorados.Add($"{arquivo.FileName}: extensão inválida");
                continue;
            }

            var destino = Path.Combine(diretorio, Path.GetFileName(arquivo.FileName));
            await using var fs = File.Create(destino);
            await arquivo.CopyToAsync(fs, cancellationToken);
            salvos++;
        }

        return (new NfeUploadResponse(produtorId, diretorio, arquivos.Count, salvos, ignorados), null);
    }

    public async Task<(NfeProcessarResponse? response, string? error)> ProcessarAsync(NfeProcessarRequest request, CancellationToken cancellationToken)
    {
        var produtor = await _produtorRepository.GetByIdAsync(request.ProdutorId);
        if (produtor is null)
        {
            return (null, "Produtor não encontrado.");
        }

        var cpfProdutor = NormalizarSomenteDigitos(produtor.Cpf);
        if (cpfProdutor.Length != 11)
        {
            return (null, "CPF do produtor inválido. Atualize o cadastro antes de importar.");
        }

        if (request.Reprocessar)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Lancamentos WHERE ProdutorId = {request.ProdutorId}", cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM NotasFiscais WHERE ProdutorId = {request.ProdutorId}", cancellationToken);
        }

        var notasDto = await _nfeImportService.ImportarNotasAsync(cpfProdutor, request.ProdutorId);
        var pasta = await _nfeImportService.ObterCaminoPastaProdutorAsync(cpfProdutor);
        var arquivosHtml = Directory.Exists(pasta) ? Directory.GetFiles(pasta, "*.html", SearchOption.TopDirectoryOnly).Length : 0;

        var notasImportadas = 0;
        var notasJaExistiam = 0;
        var lancamentosCriados = 0;

        foreach (var notaDto in notasDto)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var notaExistente = await _notaFiscalRepository.GetByChaveAcessoAsync(notaDto.Chave);
            if (notaExistente is not null)
            {
                notasJaExistiam++;
                continue;
            }

            var produtorEhEmitente = cpfProdutor == notaDto.EmitenteCpfCnpj;
            var produtorEhDestinatario = cpfProdutor == notaDto.DestinatarioCpfCnpj;
            var temConflito = (notaDto.Tipo == TipoLancamento.Receita && produtorEhDestinatario) ||
                              (notaDto.Tipo == TipoLancamento.Despesa && produtorEhEmitente);

            string origem;
            string destino;
            string clienteFornecedor;

            if (temConflito && produtorEhDestinatario && notaDto.Tipo == TipoLancamento.Receita)
            {
                origem = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                destino = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                clienteFornecedor = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
            }
            else if (temConflito && produtorEhEmitente && notaDto.Tipo == TipoLancamento.Despesa)
            {
                origem = LimitarTexto(produtor.Nome, 200, notaDto.EmitenteNome);
                destino = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                clienteFornecedor = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
            }
            else if (produtorEhEmitente)
            {
                origem = LimitarTexto(produtor.Nome, 200, notaDto.EmitenteNome);
                destino = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
                clienteFornecedor = LimitarTexto(notaDto.DestinatarioNome, 200, "Cliente");
            }
            else if (produtorEhDestinatario)
            {
                origem = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                destino = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
                clienteFornecedor = LimitarTexto(notaDto.EmitenteNome, 200, "Fornecedor");
            }
            else
            {
                origem = LimitarTexto(notaDto.EmitenteNome, 200, "N/D");
                destino = LimitarTexto(notaDto.DestinatarioNome, 200, "N/D");
                clienteFornecedor = LimitarTexto(notaDto.Tipo == TipoLancamento.Receita ? notaDto.DestinatarioNome : notaDto.EmitenteNome, 200, "NFe");
            }

            var notaFiscal = CriarNotaFiscal(notaDto, request.ProdutorId, origem, destino);
            await _notaFiscalRepository.AddAsync(notaFiscal);
            notasImportadas++;

            var lancamento = CriarLancamento(notaDto, request.ProdutorId, notaFiscal.NumeroDaNota, clienteFornecedor);
            await _lancamentoRepository.AddAsync(lancamento);
            lancamentosCriados++;
        }

        var notasIgnoradasPorRegra = Math.Max(0, arquivosHtml - notasDto.Count);
        var mensagens = new List<string>
        {
            $"Arquivos HTML encontrados: {arquivosHtml}",
            $"Notas processadas com sucesso: {notasImportadas}",
            $"Lançamentos criados: {lancamentosCriados}",
            $"Notas já existentes: {notasJaExistiam}",
            $"Notas ignoradas por regra: {notasIgnoradasPorRegra}"
        };

        return (
            new NfeProcessarResponse(
                request.ProdutorId,
                arquivosHtml,
                notasDto.Count,
                notasImportadas,
                notasJaExistiam,
                notasIgnoradasPorRegra,
                lancamentosCriados,
                mensagens),
            null);
    }

    private static NotaFiscal CriarNotaFiscal(NotaFiscalDTO notaDto, int produtorId, string origem, string destino)
    {
        var numeroFallback = string.IsNullOrWhiteSpace(notaDto.Chave)
            ? Guid.NewGuid().ToString("N")[..9]
            : notaDto.Chave[^Math.Min(9, notaDto.Chave.Length)..];

        return new NotaFiscal
        {
            ChaveAcesso = notaDto.Chave,
            ProdutorId = produtorId,
            TipoNota = notaDto.Tipo == TipoLancamento.Receita ? TipoNota.Saida : TipoNota.Entrada,
            NumeroDaNota = LimitarTexto(notaDto.NumeroNota, 20, numeroFallback),
            DataEmissao = notaDto.DataEmissao ?? DateTime.Now,
            ValorNotaFiscal = notaDto.ValorTotal,
            Origem = origem,
            Destino = destino,
            Descricao = LimitarTexto(notaDto.Descricao, 500, "Importado automaticamente da SEFAZ-MS"),
            NaturezaOperacao = LimitarTexto(notaDto.Natureza, 1000),
            Cfops = LimitarTexto(notaDto.CFOP, 500),
            ItensDescricao = LimitarTexto(notaDto.Descricao, 2000)
        };
    }

    private static Lancamento CriarLancamento(NotaFiscalDTO notaDto, int produtorId, string numeroNota, string clienteFornecedor)
    {
        return new Lancamento
        {
            Tipo = notaDto.Tipo,
            ProdutorId = produtorId,
            ClienteFornecedor = clienteFornecedor,
            Descricao = LimitarTexto($"{notaDto.Descricao} - NF {numeroNota}", 500, "Lançamento importado de NF-e"),
            Situacao = "Confirmado",
            Valor = notaDto.ValorTotal,
            Data = notaDto.DataEmissao ?? DateTime.Now,
            Vencimento = notaDto.DataEmissao ?? DateTime.Now
        };
    }

    private static string NormalizarSomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return ApenasDigitosRegex.Replace(valor, string.Empty);
    }

    private static string LimitarTexto(string? valor, int tamanhoMaximo, string fallback = "")
    {
        var texto = string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
        if (texto.Length <= tamanhoMaximo)
        {
            return texto;
        }

        return texto[..tamanhoMaximo];
    }
}
