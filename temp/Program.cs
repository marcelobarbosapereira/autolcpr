using AutoLCPR.Domain.Entities;
using AutoLCPR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var applyChanges = args.Any(item => item.Equals("--apply", StringComparison.OrdinalIgnoreCase));

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appData, "AutoLCPR", "data", "autolcpr.db");
        var connectionString = $"Data Source={dbPath}";

        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"Banco nao encontrado em: {dbPath}");
            return 1;
        }

        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options);

        var notasDashboard = await context.NotasFiscais
            .AsNoTracking()
            .Where(item => item.TipoNota == TipoNota.Entrada || item.TipoNota == TipoNota.Saida)
            .Select(item => new NotaBase
            {
                ProdutorId = item.ProdutorId,
                Tipo = item.TipoNota == TipoNota.Saida ? TipoLancamento.Receita : TipoLancamento.Despesa,
                Data = item.DataEmissao.Date,
                Valor = decimal.Round(item.ValorNotaFiscal, 2)
            })
            .ToListAsync();

        var chavesDashboard = BuildMultiset(notasDashboard);

        var lancamentos = await context.Lancamentos
            .Where(item => item.Tipo == TipoLancamento.Receita || item.Tipo == TipoLancamento.Despesa)
            .ToListAsync();

        var lancamentosOrfaos = new List<Lancamento>();
        foreach (var item in lancamentos.OrderBy(item => item.Data).ThenBy(item => item.Id))
        {
            var chave = new RegistroKey(item.ProdutorId, item.Tipo, item.Data.Date, decimal.Round(item.Valor, 2));
            if (chavesDashboard.TryGetValue(chave, out var saldo) && saldo > 0)
            {
                chavesDashboard[chave] = saldo - 1;
                continue;
            }

            lancamentosOrfaos.Add(item);
        }

        var resumo = lancamentosOrfaos
            .GroupBy(item => new { item.ProdutorId, item.Tipo })
            .OrderBy(item => item.Key.ProdutorId)
            .ThenBy(item => item.Key.Tipo)
            .Select(item => new
            {
                item.Key.ProdutorId,
                item.Key.Tipo,
                Quantidade = item.Count(),
                Valor = item.Sum(x => x.Valor)
            })
            .ToList();

        Console.WriteLine($"Banco: {dbPath}");
        Console.WriteLine($"Notas (base Dashboard): {notasDashboard.Count}");
        Console.WriteLine($"Lancamentos financeiros: {lancamentos.Count}");
        Console.WriteLine($"Lancamentos orfaos vs Dashboard: {lancamentosOrfaos.Count}");

        if (resumo.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Resumo por produtor/tipo:");
            foreach (var item in resumo)
            {
                Console.WriteLine($"- ProdutorId={item.ProdutorId} | Tipo={item.Tipo} | Qtd={item.Quantidade} | Valor={item.Valor:N2}");
            }
        }

        if (!applyChanges)
        {
            Console.WriteLine();
            Console.WriteLine("Modo simulacao. Use --apply para excluir os orfaos.");
            return 0;
        }

        if (lancamentosOrfaos.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Nenhum registro orfao para excluir.");
            return 0;
        }

        context.Lancamentos.RemoveRange(lancamentosOrfaos);
        await context.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine($"Exclusao concluida. Registros removidos: {lancamentosOrfaos.Count}");
        return 0;
    }

    private static Dictionary<RegistroKey, int> BuildMultiset(IEnumerable<NotaBase> itens)
    {
        var resultado = new Dictionary<RegistroKey, int>();
        foreach (var item in itens)
        {
            var chave = new RegistroKey(item.ProdutorId, item.Tipo, item.Data, item.Valor);
            resultado.TryGetValue(chave, out var quantidade);
            resultado[chave] = quantidade + 1;
        }

        return resultado;
    }

    private sealed class NotaBase
    {
        public int ProdutorId { get; init; }
        public TipoLancamento Tipo { get; init; }
        public DateTime Data { get; init; }
        public decimal Valor { get; init; }
    }

    private sealed record RegistroKey(int ProdutorId, TipoLancamento Tipo, DateTime Data, decimal Valor);
}
