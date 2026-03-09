using AutoLCPR.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<NfeConfigService>();
        services.AddScoped<NfeImportService>();
        return services;
    }
}
