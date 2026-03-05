using AutoLCPR.Application.Relatorios;
using AutoLCPR.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRelatorioAnualService, RelatorioAnualService>();
        services.AddScoped<IRelatorioRebanhoService, RelatorioRebanhoService>();
        services.AddScoped<IRelatorioFinanceiroService, RelatorioFinanceiroService>();
        services.AddSingleton<NfeConfigService>();
        services.AddScoped<NfeImportService>();
        return services;
    }
}
