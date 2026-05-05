using AutoLCPR.Application.Services;

var pdf = args.Length > 0 ? args[0] : @"C:\Users\marce\Desktop\1777927457172.pdf";
if (!File.Exists(pdf))
{
    Console.WriteLine($"PDF nao encontrado: {pdf}");
    return;
}

var parser = new ExtratoRebanhoPdfParserService();
var dto = await parser.ParseAsync(pdf);

Console.WriteLine($"Saldo inicial: {dto.SaldoInicial}");
Console.WriteLine($"Nascimentos: {dto.Nascimentos}");
Console.WriteLine($"Mortes/Consumos: {dto.MortesConsumos}");
Console.WriteLine($"Entradas: {dto.Entradas}");
Console.WriteLine($"Saidas: {dto.Saidas}");
Console.WriteLine($"Saldo final: {dto.SaldoFinal}");
