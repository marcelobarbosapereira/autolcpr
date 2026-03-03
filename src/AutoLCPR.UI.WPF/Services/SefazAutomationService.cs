using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AutoLCPR.UI.WPF.Services;

public class SefazAutomationService : ISefazAutomationService
{
    private static readonly Regex ChaveRegex = new("\\d{44}", RegexOptions.Compiled);

    public async Task<SefazAutomationResult> ExecutarImportacaoAsync(SefazAutomationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DataInicio.Date > request.DataFim.Date)
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = "Período inválido: DataInicio não pode ser maior que DataFim."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = "Usuário e senha são obrigatórios para autenticação na SEFAZ-MS."
            };
        }

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = request.Headless
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync(request.UrlLogin, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = request.TimeoutMs
            });

            await page.FillAsync(request.UsuarioSelector, request.Usuario);
            await page.FillAsync(request.SenhaSelector, request.Senha);
            await page.ClickAsync(request.BotaoLoginSelector);

            if (!string.IsNullOrWhiteSpace(request.SeletorElementoPosLogin))
            {
                await page.WaitForSelectorAsync(request.SeletorElementoPosLogin, new PageWaitForSelectorOptions
                {
                    Timeout = request.TimeoutMs
                });
            }

            if (!string.IsNullOrWhiteSpace(request.UrlConsulta))
            {
                await page.GotoAsync(request.UrlConsulta, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = request.TimeoutMs
                });
            }

            await page.FillAsync(request.DataInicioSelector, request.DataInicio.ToString("dd/MM/yyyy"));
            await page.FillAsync(request.DataFimSelector, request.DataFim.ToString("dd/MM/yyyy"));
            await page.ClickAsync(request.BotaoPesquisarSelector);

            await page.WaitForSelectorAsync(request.TabelaResultadoSelector, new PageWaitForSelectorOptions
            {
                Timeout = request.TimeoutMs
            });

            var celulas = await page.QuerySelectorAllAsync(request.ColunaChaveSelector);
            var chaves = new List<string>();

            foreach (var celula in celulas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var texto = (await celula.InnerTextAsync()).Trim();
                var matches = ChaveRegex.Matches(texto);
                foreach (Match match in matches)
                {
                    if (!chaves.Contains(match.Value))
                    {
                        chaves.Add(match.Value);
                    }
                }
            }

            await context.CloseAsync();

            return new SefazAutomationResult
            {
                Sucesso = true,
                Mensagem = $"Importação concluída. {chaves.Count} chave(s) encontrada(s).",
                ChavesAcesso = chaves
            };
        }
        catch (OperationCanceledException)
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = "Operação de importação cancelada pelo usuário."
            };
        }
        catch (TimeoutException ex)
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = $"Tempo excedido durante automação da SEFAZ-MS: {ex.Message}"
            };
        }
        catch (PlaywrightException ex)
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = $"Erro Playwright: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new SefazAutomationResult
            {
                Sucesso = false,
                Mensagem = $"Falha inesperada durante importação: {ex.Message}"
            };
        }
    }

    public async Task<List<string>> ObterChavesAcessoAsync(SefazAutomationRequest request, CancellationToken cancellationToken = default)
    {
        var resultado = await ExecutarImportacaoAsync(request, cancellationToken);
        if (!resultado.Sucesso)
        {
            throw new InvalidOperationException(resultado.Mensagem);
        }

        return resultado.ChavesAcesso;
    }
}
