using AutoLCPR.Application.DTOs;

namespace AutoLCPR.Application.Services;

public interface IExtratoRebanhoPdfParserService
{
    Task<ExtratoRebanhoPdfDTO> ParseAsync(string caminhoPdf, CancellationToken cancellationToken = default);
}
