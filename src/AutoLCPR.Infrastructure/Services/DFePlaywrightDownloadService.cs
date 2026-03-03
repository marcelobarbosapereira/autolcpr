using System.Text.RegularExpressions;
using AutoLCPR.Application.DFe;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.Playwright;

namespace AutoLCPR.Infrastructure.Services;

public class DFePlaywrightDownloadService : IDFePlaywrightDownloadService
{
    private const string PortalNfeUrl = "https://www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx?tipoConsulta=resumo";
    private const int MaxTentativasPorChave = 3;
    private const int TimeoutOperacaoMs = 60000;

    private static readonly string[] ChaveInputSelectors =
    {
        "input[name='chNFe']",
        "#chaveAcesso",
        "#txtChaveAcesso",
        "input[id*='chave']",
        "input[name*='chave']"
    };

    private static readonly string[] ConsultarButtonSelectors =
    {
        "button:has-text('Continuar')",
        "button:has-text('Consultar')",
        "input[type='submit'][value*='Consultar']",
        "#btnConsultar",
        "#btnConsulta"
    };

    private static readonly string[] DownloadButtonSelectors =
    {
        "a:has-text('Download do documento')",
        "button:has-text('Download do documento')",
        "a:has-text('Baixar XML')",
        "button:has-text('Baixar XML')",
        "#btnDownload",
        "a[id*='download']",
        "button[id*='download']"
    };

    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly IProdutorRepository _produtorRepository;

    public DFePlaywrightDownloadService(
        INotaFiscalRepository notaFiscalRepository,
        IProdutorRepository produtorRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
        _produtorRepository = produtorRepository;
    }

    public async Task BaixarXmlPendentesAsync(int produtorId, IProgress<DownloadXmlProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DFe] Iniciando download de XML pendentes para ProdutorId={produtorId}");

        var produtor = await _produtorRepository.GetByIdAsync(produtorId);
        if (produtor == null)
        {
            Console.WriteLine($"[DFe] Produtor {produtorId} não encontrado.");
            progress?.Report(new DownloadXmlProgress
            {
                TotalNotas = 0,
                Processadas = 0,
                Sucesso = 0,
                Falhas = 0,
                Mensagem = $"Produtor {produtorId} não encontrado."
            });
            return;
        }

        var cnpjPasta = ObterCnpjParaPasta(produtor);
        var notasDoProdutor = (await _notaFiscalRepository.GetByProdutorIdAsync(produtorId)).ToList();

        var notasPendentes = notasDoProdutor
            .Where(n => !string.IsNullOrWhiteSpace(n.ChaveAcesso))
            .Where(n => n.ChaveAcesso!.Length == 44)
            .Where(n => !n.XmlBaixado)
            .ToList();

        if (notasPendentes.Count == 0)
        {
            Console.WriteLine($"[DFe] Nenhuma nota pendente para ProdutorId={produtorId}.");
            progress?.Report(new DownloadXmlProgress
            {
                TotalNotas = 0,
                Processadas = 0,
                Sucesso = 0,
                Falhas = 0,
                Mensagem = "Nenhuma nota pendente para baixar XML."
            });
            return;
        }

        progress?.Report(new DownloadXmlProgress
        {
            TotalNotas = notasPendentes.Count,
            Processadas = 0,
            Sucesso = 0,
            Falhas = 0,
            Mensagem = $"Iniciando download de {notasPendentes.Count} XML(s)..."
        });

        var profileDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoLCPR",
            "playwright",
            "dfe-profile");

        Directory.CreateDirectory(profileDir);

        using var playwright = await Playwright.CreateAsync();
        await using var context = await playwright.Chromium.LaunchPersistentContextAsync(profileDir, new BrowserTypeLaunchPersistentContextOptions
        {
            AcceptDownloads = true,
            Headless = false,
            Channel = "msedge"
        });

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
        await page.GotoAsync(PortalNfeUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = TimeoutOperacaoMs
        });

        var total = notasPendentes.Count;
        var processadas = 0;
        var sucessos = 0;
        var falhas = 0;

        foreach (var notaResumo in notasPendentes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var nota = await _notaFiscalRepository.GetByIdAsync(notaResumo.Id);
                if (nota == null)
                {
                    Console.WriteLine($"[DFe] Nota {notaResumo.Id} não encontrada na carga de processamento.");
                    processadas++;
                    falhas++;
                    progress?.Report(new DownloadXmlProgress
                    {
                        TotalNotas = total,
                        Processadas = processadas,
                        Sucesso = sucessos,
                        Falhas = falhas,
                        Mensagem = $"Nota {notaResumo.Id} não encontrada."
                    });
                    continue;
                }

                var processouComSucesso = await ProcessarNotaComRetryAsync(page, nota, cnpjPasta);

                processadas++;
                if (processouComSucesso)
                {
                    sucessos++;
                }
                else
                {
                    falhas++;
                    Console.WriteLine($"[DFe] Falha definitiva para nota Id={nota.Id} Chave={nota.ChaveAcesso}.");
                }

                progress?.Report(new DownloadXmlProgress
                {
                    TotalNotas = total,
                    Processadas = processadas,
                    Sucesso = sucessos,
                    Falhas = falhas,
                    Mensagem = $"Processadas {processadas}/{total} notas."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DFe] Erro não tratado ao processar nota Id={notaResumo.Id}: {ex.Message}");

                processadas++;
                falhas++;
                progress?.Report(new DownloadXmlProgress
                {
                    TotalNotas = total,
                    Processadas = processadas,
                    Sucesso = sucessos,
                    Falhas = falhas,
                    Mensagem = $"Erro ao processar nota {notaResumo.Id}: {TruncarMensagem(ex.Message, 90)}"
                });
            }
        }

        progress?.Report(new DownloadXmlProgress
        {
            TotalNotas = total,
            Processadas = processadas,
            Sucesso = sucessos,
            Falhas = falhas,
            Mensagem = $"Download concluído. Sucesso: {sucessos}, Falhas: {falhas}."
        });

        Console.WriteLine($"[DFe] Download de XML finalizado para ProdutorId={produtorId}");
    }

    private async Task<bool> ProcessarNotaComRetryAsync(IPage page, NotaFiscal nota, string cnpjPasta)
    {
        for (var tentativa = 1; tentativa <= MaxTentativasPorChave; tentativa++)
        {
            try
            {
                nota.StatusDownload = $"Processando (tentativa {tentativa}/{MaxTentativasPorChave})";
                nota.UpdatedAt = DateTime.Now;
                await _notaFiscalRepository.UpdateAsync(nota);

                Console.WriteLine($"[DFe] Processando chave {nota.ChaveAcesso} | tentativa {tentativa}/{MaxTentativasPorChave}");

                var caminhoSalvo = await ProcessarNotaAsync(page, nota, cnpjPasta);

                nota.XmlBaixado = true;
                nota.StatusDownload = "Baixado";
                nota.DataDownload = DateTime.Now;
                nota.CaminhoXml = caminhoSalvo;
                nota.UpdatedAt = DateTime.Now;
                await _notaFiscalRepository.UpdateAsync(nota);

                Console.WriteLine($"[DFe] XML salvo com sucesso em: {caminhoSalvo}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DFe] Erro na tentativa {tentativa} da chave {nota.ChaveAcesso}: {ex.Message}");

                nota.XmlBaixado = false;
                nota.StatusDownload = tentativa >= MaxTentativasPorChave
                    ? $"Falha após {MaxTentativasPorChave} tentativas: {TruncarMensagem(ex.Message)}"
                    : $"Falha tentativa {tentativa}: {TruncarMensagem(ex.Message)}";
                nota.UpdatedAt = DateTime.Now;
                await _notaFiscalRepository.UpdateAsync(nota);

                if (tentativa >= MaxTentativasPorChave)
                {
                    return false;
                }

                await page.GotoAsync(PortalNfeUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = TimeoutOperacaoMs
                });
            }
        }

        return false;
    }

    private async Task<string> ProcessarNotaAsync(IPage page, NotaFiscal nota, string cnpjPasta)
    {
        if (string.IsNullOrWhiteSpace(nota.ChaveAcesso))
        {
            throw new InvalidOperationException("Nota fiscal sem chave de acesso.");
        }

        var inputChaveSelector = await ResolverSeletorDisponivelAsync(page, ChaveInputSelectors);
        if (inputChaveSelector == null)
        {
            throw new InvalidOperationException("Campo de chave de acesso não encontrado na página do portal NF-e.");
        }

        var botaoConsultarSelector = await ResolverSeletorDisponivelAsync(page, ConsultarButtonSelectors);
        if (botaoConsultarSelector == null)
        {
            throw new InvalidOperationException("Botão de consulta não encontrado na página do portal NF-e.");
        }

        await page.FillAsync(inputChaveSelector, string.Empty);
        await page.FillAsync(inputChaveSelector, nota.ChaveAcesso);
        await page.ClickAsync(botaoConsultarSelector);

        var botaoDownloadSelector = await AguardarSeletorDisponivelAsync(page, DownloadButtonSelectors, TimeoutOperacaoMs);
        if (botaoDownloadSelector == null)
        {
            throw new TimeoutException("Botão de download do XML não ficou disponível após a consulta.");
        }

        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync(botaoDownloadSelector);
        }, new PageRunAndWaitForDownloadOptions
        {
            Timeout = TimeoutOperacaoMs
        });

        var ano = nota.DataEmissao.Year.ToString("0000");
        var pastaDestino = Path.Combine(AppContext.BaseDirectory, "XML", cnpjPasta, ano);
        Directory.CreateDirectory(pastaDestino);

        var caminhoDestino = Path.Combine(pastaDestino, $"{nota.ChaveAcesso}.xml");
        await download.SaveAsAsync(caminhoDestino);

        return caminhoDestino;
    }

    private static async Task<string?> ResolverSeletorDisponivelAsync(IPage page, IEnumerable<string> seletores)
    {
        foreach (var seletor in seletores)
        {
            var elemento = await page.QuerySelectorAsync(seletor);
            if (elemento != null)
            {
                return seletor;
            }
        }

        return null;
    }

    private static async Task<string?> AguardarSeletorDisponivelAsync(IPage page, IEnumerable<string> seletores, float timeoutMs)
    {
        foreach (var seletor in seletores)
        {
            try
            {
                await page.WaitForSelectorAsync(seletor, new PageWaitForSelectorOptions
                {
                    Timeout = timeoutMs,
                    State = WaitForSelectorState.Visible
                });

                return seletor;
            }
            catch
            {
            }
        }

        return null;
    }

    private static string ObterCnpjParaPasta(Produtor produtor)
    {
        var apenasDigitos = Regex.Replace(produtor.InscricaoEstadual ?? string.Empty, "\\D", string.Empty);

        if (apenasDigitos.Length == 14)
        {
            return apenasDigitos;
        }

        if (apenasDigitos.Length > 0)
        {
            return apenasDigitos;
        }

        return "SEM_CNPJ";
    }

    private static string TruncarMensagem(string? mensagem, int limite = 180)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            return "erro não detalhado";
        }

        return mensagem.Length <= limite
            ? mensagem
            : mensagem[..limite];
    }
}
