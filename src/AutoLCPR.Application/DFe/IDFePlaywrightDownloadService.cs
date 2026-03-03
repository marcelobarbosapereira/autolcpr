namespace AutoLCPR.Application.DFe;

public interface IDFePlaywrightDownloadService
{
    Task BaixarXmlPendentesAsync(int produtorId, IProgress<DownloadXmlProgress>? progress = null, CancellationToken cancellationToken = default);
}

public sealed class DownloadXmlProgress
{
    public int TotalNotas { get; init; }
    public int Processadas { get; init; }
    public int Sucesso { get; init; }
    public int Falhas { get; init; }
    public string Mensagem { get; init; } = string.Empty;
}
