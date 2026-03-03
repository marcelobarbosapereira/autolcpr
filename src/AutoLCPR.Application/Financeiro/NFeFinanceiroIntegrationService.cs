using System.Text.RegularExpressions;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;

namespace AutoLCPR.Application.Financeiro;

public sealed class NFeFinanceiroIntegrationService : INFeFinanceiroIntegrationService
{
    private static readonly Regex ApenasDigitosRegex = new("\\D", RegexOptions.Compiled);

    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public NFeFinanceiroIntegrationService(
        INotaFiscalRepository notaFiscalRepository,
        ILancamentoRepository lancamentoRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task IntegrarNotaAsync(NotaFiscalDTO nota, Produtor produtor)
    {
        if (nota is null)
        {
            throw new ArgumentNullException(nameof(nota));
        }

        if (produtor is null)
        {
            throw new ArgumentNullException(nameof(produtor));
        }

        if (produtor.Id <= 0)
        {
            throw new InvalidOperationException("O produtor precisa estar persistido antes da integração financeira.");
        }

        if (string.IsNullOrWhiteSpace(nota.NumeroNF))
        {
            throw new InvalidOperationException("NumeroNF é obrigatório para integração financeira.");
        }

        if (nota.Itens.Count == 0)
        {
            return;
        }

        var cnpjProdutor = SanitizarDocumento(produtor.InscricaoEstadual);
        var cnpjEmitente = SanitizarDocumento(nota.Emitente.Cnpj);
        var cnpjDestinatario = SanitizarDocumento(nota.Destinatario.Cnpj);

        var produtorEhEmitente = cnpjProdutor.Length > 0 && cnpjProdutor == cnpjEmitente;
        var produtorEhDestinatario = cnpjProdutor.Length > 0 && cnpjProdutor == cnpjDestinatario;

        if (!produtorEhEmitente && !produtorEhDestinatario)
        {
            return;
        }

        var notaFiscal = await ObterOuCriarNotaFiscalAsync(nota, produtor, produtorEhEmitente);

        var notaJaProcessada = await _lancamentoRepository.ExistePorNotaFiscalIdAsync(notaFiscal.Id);
        if (notaJaProcessada)
        {
            return;
        }

        if (produtorEhEmitente)
        {
            await CriarReceitaAsync(nota, produtor, notaFiscal);
        }

        if (produtorEhDestinatario)
        {
            await CriarDespesaAsync(nota, produtor, notaFiscal);
        }
    }

    private async Task CriarDespesaAsync(NotaFiscalDTO nota, Produtor produtor, NotaFiscal notaFiscal)
    {
        var origem = string.IsNullOrWhiteSpace(nota.Emitente.Nome) ? "Emitente não informado" : nota.Emitente.Nome.Trim();

        var lancamentos = nota.Itens
            .Where(item => item.Quantidade > 0 && item.ValorUnitario > 0)
            .Select(item => new Lancamento
            {
                Data = nota.DataEmissao,
                Tipo = TipoLancamento.Despesa,
                ClienteFornecedor = origem,
                Descricao = string.IsNullOrWhiteSpace(item.Descricao) ? "Item sem descrição" : item.Descricao.Trim(),
                Situacao = "Concluído",
                Valor = item.Quantidade * item.ValorUnitario,
                Vencimento = nota.DataEmissao,
                ProdutorId = produtor.Id,
                NotaFiscalId = notaFiscal.Id
            })
            .ToList();

        if (lancamentos.Count == 0)
        {
            return;
        }

        await _lancamentoRepository.AddRangeAsync(lancamentos);
    }

    private async Task CriarReceitaAsync(NotaFiscalDTO nota, Produtor produtor, NotaFiscal notaFiscal)
    {
        var origem = string.IsNullOrWhiteSpace(nota.Destinatario.Nome) ? "Destinatário não informado" : nota.Destinatario.Nome.Trim();

        var lancamentos = nota.Itens
            .Where(item => item.Quantidade > 0 && item.ValorUnitario > 0)
            .Select(item => new Lancamento
            {
                Data = nota.DataEmissao,
                Tipo = TipoLancamento.Receita,
                ClienteFornecedor = origem,
                Descricao = string.IsNullOrWhiteSpace(item.Descricao) ? "Item sem descrição" : item.Descricao.Trim(),
                Situacao = "Concluído",
                Valor = item.Quantidade * item.ValorUnitario,
                Vencimento = nota.DataEmissao,
                ProdutorId = produtor.Id,
                NotaFiscalId = notaFiscal.Id
            })
            .ToList();

        if (lancamentos.Count == 0)
        {
            return;
        }

        await _lancamentoRepository.AddRangeAsync(lancamentos);
    }

    private async Task<NotaFiscal> ObterOuCriarNotaFiscalAsync(NotaFiscalDTO nota, Produtor produtor, bool produtorEhEmitente)
    {
        NotaFiscal? notaFiscal = null;

        if (!string.IsNullOrWhiteSpace(nota.ChaveAcesso))
        {
            notaFiscal = await _notaFiscalRepository.GetByChaveAcessoAsync(nota.ChaveAcesso);
        }

        if (notaFiscal is null)
        {
            var notasDoProdutor = await _notaFiscalRepository.GetByProdutorIdAsync(produtor.Id);
            notaFiscal = notasDoProdutor.FirstOrDefault(item =>
                item.NumeroDaNota == nota.NumeroNF.Trim() &&
                item.DataEmissao.Date == nota.DataEmissao.Date);
        }

        if (notaFiscal is not null)
        {
            return notaFiscal;
        }

        var valorTotal = nota.Itens.Sum(item => item.Quantidade * item.ValorUnitario);

        var entidade = new NotaFiscal
        {
            ChaveAcesso = string.IsNullOrWhiteSpace(nota.ChaveAcesso) ? null : nota.ChaveAcesso.Trim(),
            DataEmissao = nota.DataEmissao,
            NumeroDaNota = nota.NumeroNF.Trim(),
            ValorNotaFiscal = valorTotal,
            Origem = string.IsNullOrWhiteSpace(nota.Emitente.Nome) ? "Emitente não informado" : nota.Emitente.Nome.Trim(),
            Destino = string.IsNullOrWhiteSpace(nota.Destinatario.Nome) ? "Destinatário não informado" : nota.Destinatario.Nome.Trim(),
            Descricao = "Integração automática NF-e",
            TipoNota = produtorEhEmitente ? TipoNota.Saida : TipoNota.Entrada,
            ProdutorId = produtor.Id
        };

        var idNota = await _notaFiscalRepository.AddAsync(entidade);
         return await _notaFiscalRepository.GetByIdAsync(idNota)
             ?? throw new InvalidOperationException("Não foi possível recuperar a Nota Fiscal após persistência.");
    }

    private static string SanitizarDocumento(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return string.Empty;
        }

        return ApenasDigitosRegex.Replace(documento, string.Empty);
    }
}