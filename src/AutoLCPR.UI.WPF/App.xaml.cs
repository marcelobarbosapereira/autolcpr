using System.Windows;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoLCPR.Infrastructure.Data;
using AutoLCPR.UI.WPF.Services;

namespace AutoLCPR.UI.WPF
{
    public partial class App : System.Windows.Application
    {
        private IServiceProvider? _serviceProvider;
        private IConfiguration? _configuration;

        public IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider ainda nao foi inicializado.");
                }

                return _serviceProvider;
            }
        }

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            try
            {
                InitializeConfiguration();
                InitializeServices();
            }
            catch (Exception ex)
            {
                LogCriticalError("Erro durante inicialização da aplicação", ex);
                MessageBox.Show(
                    $"Erro crítico durante inicialização:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erro de Inicialização",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogCriticalError("Exceção não tratada no Dispatcher", e.Exception);
            MessageBox.Show(
                $"Erro não tratado:\n\n{e.Exception.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = false;
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogCriticalError("Exceção não tratada no domínio de aplicação", ex);
        }

        private void LogCriticalError(string message, Exception? ex)
        {
            try
            {
                SimpleLogger.LogError(message, ex);
            }
            catch
            {
                // Se o SimpleLogger falhar, pelo menos tentar escrever um arquivo
                try
                {
                    var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoLCPR");
                    Directory.CreateDirectory(logDir);
                    var logPath = Path.Combine(logDir, "error.log");
                    File.AppendAllText(logPath, $"\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n{ex}\n");
                }
                catch { }
            }
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
            AutoLCPR.Application.DependencyInjectionExtensions.AddApplicationServices(services);

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
                                    AlertService.Show(
                                        $"Erro ao aplicar migrations: {ex.InnerException?.Message ?? ex.Message}",
                                        "Erro de Migracao",
                                        AlertType.Error);
                                }
                            }
                            else
                            {
                                AlertService.Show(
                                    "Falha ao conectar ao banco de dados. Verifique a configuracao.",
                                    "Erro de Conexao",
                                    AlertType.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            AlertService.Show(
                                $"Erro durante teste de conexao: {ex.InnerException?.Message ?? ex.Message}",
                                "Erro",
                                AlertType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AlertService.Show(
                    $"Erro ao inicializar servicos: {ex.Message}",
                    "Erro",
                    AlertType.Error);
            }
        }
    }
}