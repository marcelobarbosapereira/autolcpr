using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Application.Financeiro;

public interface INFeFinanceiroIntegrationService
{
    Task IntegrarNotaAsync(NotaFiscalDTO nota, Produtor produtor);
}