using Microsoft.EntityFrameworkCore;
using AutoLCPR.Infrastructure;

class Program
{
    static async Task Main()
    {
        var connectionString = @"Data Source=C:\Users\marce\AppData\Roaming\AutoLCPR\data\autolcpr.db";
        
        using (var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options))
        {
            var tables = await context.Database.GetDbConnection().GetSchema("Tables");
            Console.WriteLine("Tabelas criadas:");
            foreach (System.Data.DataRow row in tables.Rows)
            {
                Console.WriteLine($"  - {row["TABLE_NAME"]}");
            }

            var despesasCount = await context.Despesas.CountAsync();
            Console.WriteLine($"\nRegistros em Despesas: {despesasCount}");
        }
    }
}
