using Microsoft.EntityFrameworkCore;
using AutoLCPR.Infrastructure.Data;

var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoLCPR", "data", "autolcpr.db");
var connectionString = $"Data Source={dbPath}";

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connectionString)
    .Options;

using var context = new AppDbContext(options);

var notasAntes = await context.NotasFiscais.CountAsync();
var lancamentosAntes = await context.Lancamentos.CountAsync();

await context.Database.ExecuteSqlRawAsync("DELETE FROM Lancamentos");
await context.Database.ExecuteSqlRawAsync("DELETE FROM NotasFiscais");

var notasDepois = await context.NotasFiscais.CountAsync();
var lancamentosDepois = await context.Lancamentos.CountAsync();

Console.WriteLine($"Banco: {dbPath}");
Console.WriteLine($"NotasFiscais antes/depois: {notasAntes}/{notasDepois}");
Console.WriteLine($"Lancamentos antes/depois: {lancamentosAntes}/{lancamentosDepois}");
