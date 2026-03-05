using System.Text.Json;
using AutoLCPR.Domain.Entities;

namespace AutoLCPR.Application.Services
{
    /// <summary>
    /// Serviço para gerenciar as configurações de importação de NFes
    /// </summary>
    public class NfeConfigService
    {
        private readonly string _configPath;
        private NfeImportConfig? _config;

        public NfeConfigService()
        {
            // Caminho do arquivo de configuração na pasta da aplicação
            _configPath = Path.Combine(AppContext.BaseDirectory, "nfe_config.json");
        }

        /// <summary>
        /// Carrega as configurações do arquivo JSON
        /// </summary>
        public async Task<NfeImportConfig> CarregarConfiguracaoAsync()
        {
            if (_config != null)
            {
                return _config;
            }

            try
            {
                if (!File.Exists(_configPath))
                {
                    // Se não existir, criar com valores padrão
                    _config = CriarConfiguracaoPadrao();
                    await SalvarConfiguracaoAsync(_config);
                    return _config;
                }

                var json = await File.ReadAllTextAsync(_configPath);
                _config = JsonSerializer.Deserialize<NfeImportConfig>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? CriarConfiguracaoPadrao();

                return _config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar configurações: {ex.Message}");
                _config = CriarConfiguracaoPadrao();
                return _config;
            }
        }

        /// <summary>
        /// Salva as configurações no arquivo JSON
        /// </summary>
        public async Task SalvarConfiguracaoAsync(NfeImportConfig config)
        {
            try
            {
                var opcoes = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                var json = JsonSerializer.Serialize(config, opcoes);
                await File.WriteAllTextAsync(_configPath, json);
                _config = config;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar configurações: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Limpa o cache de configuração carregada
        /// </summary>
        public void LimparCache()
        {
            _config = null;
        }

        /// <summary>
        /// Cria uma configuração padrão
        /// </summary>
        private NfeImportConfig CriarConfiguracaoPadrao()
        {
            return new NfeImportConfig
            {
                PastaHtml = "%AppData%/AutoLCPR/html",
                ImagemCabecalho = null,
                IgnorarCFOP = new List<string>
                {
                    "5910", // Ato cooperado
                    "6910"
                },
                IgnorarNatureza = new List<string>
                {
                    "DEVOLUÇÃO",
                    "BONIFICAÇÃO"
                },
                CFOPReceita = new List<string>
                {
                    "5102", // Venda de produção própria
                    "5104",
                    "5410"
                },
                CFOPDespesa = new List<string>
                {
                    "1102", // Compra para revenda
                    "1104",
                    "7102"
                },
                NaturezaReceita = new List<string>
                {
                    "VENDA",
                    "RECEITA"
                },
                NaturezaDespesa = new List<string>
                {
                    "COMPRA",
                    "DESPESA"
                }
            };
        }
    }
}
