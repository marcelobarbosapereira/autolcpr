namespace AutoLCPR.Application.DTOs;

public class FolhaPagamentoPdfDTO
{
    public string ArquivoOrigem { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Competencia { get; set; }
    public bool ParserImplementado { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public List<FolhaPagamentoItemDTO> Itens { get; set; } = new();
}
