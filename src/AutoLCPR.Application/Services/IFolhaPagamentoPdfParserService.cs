using AutoLCPR.Application.DTOs;

namespace AutoLCPR.Application.Services;

public interface IFolhaPagamentoPdfParserService
{
    Task<FolhaPagamentoPdfDTO> ParseAsync(string caminhoPdf, CancellationToken cancellationToken = default);
}
