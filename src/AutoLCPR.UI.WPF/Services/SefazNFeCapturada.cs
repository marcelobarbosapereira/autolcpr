namespace AutoLCPR.UI.WPF.Services;

public class SefazNFeCapturada
{
    public required string ChaveAcesso { get; set; }
    public string? IdentificadorDetalhe { get; set; }
    public string? NumeroNota { get; set; }
    public DateTime? DataEmissao { get; set; }
    public decimal ValorTotal { get; set; }
    public string? CpfCnpjDestinatario { get; set; }
    public string? IeDestinatario { get; set; }
    public string? RazaoSocialDestinatario { get; set; }
    public string? CpfCnpjEmitente { get; set; }
    public string? IeEmitente { get; set; }
    public string? RazaoSocialEmitente { get; set; }
    public string? Situacao { get; set; }
    public string? NaturezaOperacao { get; set; }
    public List<string> DescricoesProdutosServicos { get; set; } = new();
    public List<string> Cfops { get; set; } = new();
    public string? HtmlConsulta { get; set; }
}
