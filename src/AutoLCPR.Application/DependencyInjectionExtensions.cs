using AutoLCPR.Application.Financeiro;
using AutoLCPR.Application.Relatorios;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRelatorioAnualService, RelatorioAnualService>();
        services.AddScoped<IRelatorioRebanhoService, RelatorioRebanhoService>();
        services.AddScoped<IRelatorioFinanceiroService, RelatorioFinanceiroService>();
        services.AddScoped<INFeFinanceiroIntegrationService, NFeFinanceiroIntegrationService>();
        return services;
    }
}
