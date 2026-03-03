namespace AutoLCPR.UI.WPF.Services;

public interface ISefazAutomationService
{
    Task<SefazAutomationResult> ExecutarImportacaoAsync(SefazAutomationRequest request, CancellationToken cancellationToken = default);
    Task<List<string>> ObterChavesAcessoAsync(SefazAutomationRequest request, CancellationToken cancellationToken = default);
}

public class SefazAutomationRequest
{
    public required string UrlLogin { get; set; }
    public string? UrlConsulta { get; set; }
    public required string Usuario { get; set; }
    public required string Senha { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string UsuarioSelector { get; set; } = "#usuario";
    public string SenhaSelector { get; set; } = "#senha";
    public string BotaoLoginSelector { get; set; } = "button[type='submit']";
    public string DataInicioSelector { get; set; } = "#dataInicio";
    public string DataFimSelector { get; set; } = "#dataFim";
    public string BotaoPesquisarSelector { get; set; } = "#btnPesquisar";
    public string TabelaResultadoSelector { get; set; } = "table tbody tr";
    public string ColunaChaveSelector { get; set; } = "table tbody tr td:nth-child(1)";
    public string? SeletorElementoPosLogin { get; set; }
    public int TimeoutMs { get; set; } = 90000;
    public bool Headless { get; set; } = false;
}

public class SefazAutomationResult
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public List<string> ChavesAcesso { get; set; } = new();
}
