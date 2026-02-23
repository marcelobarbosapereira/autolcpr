using System.Windows;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoLCPR.Infrastructure.Data;

namespace AutoLCPR.UI.WPF
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private IConfiguration? _configuration;

        public App()
        {
            InitializeConfiguration();
            InitializeServices();
        }

        /// <summary>
        /// Inicializa a configuração da aplicação
        /// </summary>
        private void InitializeConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);

            _configuration = configBuilder.Build();
        }

        /// <summary>
        /// Resolve a connection string com os caminhos necessários
        /// </summary>
        private string ResolveConnectionString()
        {
            var connString = _configuration?.GetConnectionString("DefaultConnection") 
                ?? "Data Source=autolcpr.db";
            
            // Substituir {DatabasePath} com o caminho real
            var databasePath = _configuration?["DatabasePath"] ?? "%AppData%/AutoLCPR/data";
            databasePath = Environment.ExpandEnvironmentVariables(databasePath);
            
            // Criar diretório se não existir
            try
            {
                Directory.CreateDirectory(databasePath);
                SimpleLogger.Log($"Diretório de dados criado/verificado: {databasePath}");
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError($"Erro ao criar diretório de dados", ex);
            }
            
            connString = connString.Replace("{DatabasePath}", databasePath);
            
            SimpleLogger.Log($"Connection String resolvido: {connString}");
            
            return connString;
        }

        /// <summary>
        /// Inicializa os serviços da aplicação
        /// </summary>
        private void InitializeServices()
        {
            var services = new ServiceCollection();

            // Registrar serviços de infraestrutura
            var connectionString = ResolveConnectionString();
            services.AddInfrastructureServices(connectionString);

            _serviceProvider = services.BuildServiceProvider();

            // Testar conexão com banco de dados
            TestDatabaseConnection();
        }

        /// <summary>
        /// Testa a conexão com o banco de dados
        /// </summary>
        private void TestDatabaseConnection()
        {
            try
            {
                using (var scope = _serviceProvider?.CreateScope())
                {
                    var dbContext = scope?.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (dbContext != null)
                    {
                        try
                        {
                            // Teste de conexão
                            var isConnected = DbInitializer.TestConnectionAsync(dbContext).Result;
                            
                            if (isConnected)
                            {
                                try
                                {
                                    // Inicializar banco de dados
                                    DbInitializer.InitializeAsync(dbContext).Wait();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(
                                        $"Erro ao aplicar migrations: {ex.InnerException?.Message ?? ex.Message}",
                                        "Erro de Migração",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Falha ao conectar ao banco de dados. Verifique a configuração.",
                                    "Erro de Conexão",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Erro durante teste de conexão: {ex.InnerException?.Message ?? ex.Message}",
                                "Erro",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inicializar serviços: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

