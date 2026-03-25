using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoLCPR.Infrastructure.Data;

var services = new ServiceCollection();
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoLCPR", "data", "autolcpr.db");
var connectionString = $"Data Source={dbPath}";

services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("=== RANKING DE CFOPs ===");
var cfopsRanking = await context.NotasFiscais
    .Where(n => n.Cfops != null && n.Cfops != "")
    .GroupBy(n => n.Cfops)
    .Select(g => new { Cfop = g.Key, Quantidade = g.Count() })
    .OrderByDescending(x => x.Quantidade)
    .Take(10)
    .ToListAsync();

foreach (var item in cfopsRanking)
{
    Console.WriteLine($"{item.Cfop}: {item.Quantidade} notas");
}

Console.WriteLine("\n=== RANKING DE NATUREZA DE OPERAÇÃO ===");
var naturezaRanking = await context.NotasFiscais
    .Where(n => n.NaturezaOperacao != null && n.NaturezaOperacao != "")
    .GroupBy(n => n.NaturezaOperacao)
    .Select(g => new { Natureza = g.Key, Quantidade = g.Count() })
    .OrderByDescending(x => x.Quantidade)
    .Take(10)
    .ToListAsync();

foreach (var item in naturezaRanking)
{
    Console.WriteLine($"{item.Natureza}: {item.Quantidade} notas");
}

Console.WriteLine("\n=== ESTATÍSTICAS GERAIS ===");
var total = await context.NotasFiscais.CountAsync();
var comCfop = await context.NotasFiscais.CountAsync(n => n.Cfops != null && n.Cfops != "");
var comNatureza = await context.NotasFiscais.CountAsync(n => n.NaturezaOperacao != null && n.NaturezaOperacao != "");

Console.WriteLine($"Total de Notas Fiscais: {total}");
Console.WriteLine($"Notas com CFOP: {comCfop}");
Console.WriteLine($"Notas com Natureza: {comNatureza}");
