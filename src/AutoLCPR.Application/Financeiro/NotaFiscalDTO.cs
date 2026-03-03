namespace AutoLCPR.Application.Financeiro;

public sealed class NotaFiscalDTO
{
    public string? ChaveAcesso { get; set; }
    public DateTime DataEmissao { get; set; }
    public string NumeroNF { get; set; } = string.Empty;
    public NFeParticipanteDTO Emitente { get; set; } = new();
    public NFeParticipanteDTO Destinatario { get; set; } = new();
    public IReadOnlyCollection<NotaFiscalItemDTO> Itens { get; set; } = Array.Empty<NotaFiscalItemDTO>();
}

public sealed class NFeParticipanteDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
}

public sealed class NotaFiscalItemDTO
{
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}