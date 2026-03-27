namespace AutoLCPR.Application.DTOs;

public class ExtratoRebanhoPdfDTO
{
    public string ArquivoOrigem { get; set; } = string.Empty;
    public string? NomePropriedade { get; set; }
    public string? Inscricao { get; set; }
    public int? SaldoInicial { get; set; }
    public int? Nascimentos { get; set; }
    public int? MortesConsumos { get; set; }
    public int? Entradas { get; set; }
    public int? Saidas { get; set; }
    public int? SaldoFinal { get; set; }
    public bool ParserImplementado { get; set; }
    public string Observacao { get; set; } = string.Empty;
}
