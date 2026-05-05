namespace AutoLCPR.Application.DTOs;

public class FolhaPagamentoItemDTO
{
    public string TipoCalculo { get; set; } = string.Empty;
    public string Empregado { get; set; } = string.Empty;
    public string Competencia { get; set; } = string.Empty;
    public DateTime? Vencimento { get; set; }
    public decimal Valor { get; set; }
    public string ArquivoOrigem { get; set; } = string.Empty;
}
