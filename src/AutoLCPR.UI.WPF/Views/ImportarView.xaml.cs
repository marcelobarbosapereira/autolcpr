using AutoLCPR.UI.WPF.ViewModels;
using AutoLCPR.UI.WPF.Services;
using AutoLCPR.Domain.Repositories;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para ImportarView.xaml
    /// </summary>
    public partial class ImportarView : UserControl
    {
        private static readonly Regex ChaveRegex = new("\\d{44}", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonCaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly Dictionary<string, SefazNFeCapturada> _notasCapturadasPorChave = new();
        private readonly object _capturaLock = new();
        private readonly object _capturaEventoLock = new();
        private TaskCompletionSource<bool>? _proximaCapturaTcs;
        private TaskCompletionSource<string?>? _proximoDetalheUrlTcs;

        public ImportarView()
        {
            InitializeComponent();
            DataContext = new ImportarViewModel();
            Loaded += ImportarView_Loaded;
            Unloaded += ImportarView_Unloaded;
        }

        private async void ImportarView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not ImportarViewModel viewModel)
            {
                return;
            }

            try
            {
                await SefazWebView.EnsureCoreWebView2Async();
                if (SefazWebView.CoreWebView2 == null)
                {
                    viewModel.Status = "Não foi possível inicializar o navegador interno.";
                    return;
                }

                SefazWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                SefazWebView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
                viewModel.CapturarNotasNoNavegadorAsync = CapturarNotasDaNavegacaoAsync;
                viewModel.Status = "Navegador interno pronto. Faça login na SEFAZ no painel ao lado.";
            }
            catch (Exception ex)
            {
                viewModel.Status = $"Falha ao inicializar navegador interno: {ex.Message}";
            }
        }

        private void ImportarView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SefazWebView.CoreWebView2 == null)
            {
                return;
            }

            SefazWebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            SefazWebView.CoreWebView2.WebResourceResponseReceived -= CoreWebView2_WebResourceResponseReceived;
        }

        private void AbrirSite_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not ImportarViewModel viewModel || SefazWebView.CoreWebView2 == null)
            {
                return;
            }

            SefazWebView.CoreWebView2.Navigate(ImportarViewModel.SefazUrl);
            viewModel.Status = "Site carregado no painel. Faça login e siga para a consulta.";
        }

        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Uri))
            {
                lock (_capturaEventoLock)
                {
                    if (_proximoDetalheUrlTcs != null)
                    {
                        _proximoDetalheUrlTcs.TrySetResult(e.Uri);
                        e.Handled = true;
                        return;
                    }
                }
            }

            if (SefazWebView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(e.Uri))
            {
                SefazWebView.CoreWebView2.Navigate(e.Uri);
                e.Handled = true;
            }
        }

        private async void CoreWebView2_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            try
            {
                if (e.Request.Uri.Contains("/NotaFiscalEletronica.aspx/carregarListaNFe", StringComparison.OrdinalIgnoreCase))
                {
                    var stream = await e.Response.GetContentAsync();
                    using var reader = new StreamReader(stream);
                    var responseBody = await reader.ReadToEndAsync();

                    var notas = ExtrairNotasDaRespostaCarregarLista(responseBody);
                    if (notas.Count == 0)
                    {
                        return;
                    }

                    lock (_capturaLock)
                    {
                        foreach (var nota in notas)
                        {
                            _notasCapturadasPorChave[nota.ChaveAcesso] = nota;
                        }
                    }

                    if (DataContext is ImportarViewModel viewModel)
                    {
                        viewModel.Status = $"{_notasCapturadasPorChave.Count} NF-e capturada(s) da consulta. Clique em 'Capturar e importar'.";
                    }

                    lock (_capturaEventoLock)
                    {
                        _proximaCapturaTcs?.TrySetResult(true);
                    }

                    return;
                }

                if (e.Request.Uri.Contains("/NotaFiscalEletronica.aspx/visualizarDnfe", StringComparison.OrdinalIgnoreCase))
                {
                    var stream = await e.Response.GetContentAsync();
                    using var reader = new StreamReader(stream);
                    var responseBody = await reader.ReadToEndAsync();
                    var detalheUrl = ExtrairDetalheUrlDaRespostaVisualizarDnfe(responseBody);

                    if (!string.IsNullOrWhiteSpace(detalheUrl))
                    {
                        lock (_capturaEventoLock)
                        {
                            _proximoDetalheUrlTcs?.TrySetResult(detalheUrl);
                        }
                    }

                    return;
                }

                return;
            }
            catch
            {
                lock (_capturaEventoLock)
                {
                    _proximaCapturaTcs?.TrySetResult(false);
                }
            }
        }

        private static string? ExtrairDetalheUrlDaRespostaVisualizarDnfe(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            try
            {
                using var rootDocument = JsonDocument.Parse(responseBody);
                if (!rootDocument.RootElement.TryGetProperty("d", out var dElement) || dElement.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                var innerPayload = dElement.ValueKind == JsonValueKind.String
                    ? dElement.GetString()
                    : dElement.GetRawText();

                if (string.IsNullOrWhiteSpace(innerPayload))
                {
                    return null;
                }

                if (Uri.IsWellFormedUriString(innerPayload, UriKind.Absolute) || innerPayload.StartsWith("/"))
                {
                    return innerPayload.Trim();
                }

                using var innerDocument = JsonDocument.Parse(innerPayload);

                JsonElement resultElement;
                if (innerDocument.RootElement.TryGetProperty("Result", out resultElement)
                    || innerDocument.RootElement.TryGetProperty("result", out resultElement)
                    || innerDocument.RootElement.TryGetProperty("Url", out resultElement)
                    || innerDocument.RootElement.TryGetProperty("url", out resultElement))
                {
                    var detalheUrl = resultElement.ValueKind == JsonValueKind.String
                        ? resultElement.GetString()
                        : resultElement.GetRawText();

                    return string.IsNullOrWhiteSpace(detalheUrl) ? null : detalheUrl.Trim();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<IReadOnlyList<SefazNFeCapturada>> CapturarNotasDaNavegacaoAsync()
        {
            if (SefazWebView.CoreWebView2 == null)
            {
                return Array.Empty<SefazNFeCapturada>();
            }

            lock (_capturaLock)
            {
                _notasCapturadasPorChave.Clear();
            }

            var paginacao = await ObterPaginacaoGridAsync();
            if (paginacao == null)
            {
                var chaves = await CapturarChavesDaPaginaAsync();
                return chaves
                    .Select(chave => new SefazNFeCapturada
                    {
                        ChaveAcesso = chave,
                        IdentificadorDetalhe = null,
                        NumeroNota = null,
                        DataEmissao = null,
                        ValorTotal = 0m,
                        CpfCnpjDestinatario = null,
                        IeDestinatario = null,
                        RazaoSocialDestinatario = null,
                        CpfCnpjEmitente = null,
                        IeEmitente = null,
                        RazaoSocialEmitente = null,
                        Situacao = null
                    })
                    .ToList();
            }

            var totalPaginas = Math.Max(1, paginacao.LastPage);
            for (var pagina = 1; pagina <= totalPaginas; pagina++)
            {
                var carregou = await RecarregarPaginaGridAsync(pagina);
                if (!carregou)
                {
                    continue;
                }
            }

            List<SefazNFeCapturada> capturadas;
            lock (_capturaLock)
            {
                capturadas = _notasCapturadasPorChave.Values
                    .OrderBy(item => item.DataEmissao ?? DateTime.MinValue)
                    .ToList();
            }

            await EnriquecerDetalhesNotasAsync(capturadas);

            return capturadas;
        }

        private async Task EnriquecerDetalhesNotasAsync(IReadOnlyList<SefazNFeCapturada> notas)
        {
            if (SefazWebView.CoreWebView2 == null || notas.Count == 0)
            {
                return;
            }

            var diretorioCache = await ObterDiretorioCacheHtmlProdutorAsync();

            for (var i = 0; i < notas.Count; i++)
            {
                var nota = notas[i];
                var detalhes = await ExtrairDetalhesNotaAsync(nota.ChaveAcesso, nota.IdentificadorDetalhe, diretorioCache);
                if (detalhes != null)
                {
                    nota.NaturezaOperacao = detalhes.NaturezaOperacao;
                    nota.DescricoesProdutosServicos = detalhes.DescricoesProdutosServicos;
                    nota.Cfops = detalhes.Cfops;
                    nota.HtmlConsulta = detalhes.HtmlConsulta;
                }

                if (DataContext is ImportarViewModel viewModel)
                {
                    viewModel.Status = $"Detalhando NF-e {i + 1}/{notas.Count}...";
                }
            }
        }

        private async Task<string?> ObterDiretorioCacheHtmlProdutorAsync()
        {
            var app = System.Windows.Application.Current as App;
            var serviceProvider = app?.ServiceProvider;
            if (serviceProvider == null)
            {
                return null;
            }

            var contexto = serviceProvider.GetService<ImportacaoContextoService>();
            var produtorId = contexto?.ProdutorSelecionadoId;
            if (produtorId == null || produtorId <= 0)
            {
                return null;
            }

            using var scope = serviceProvider.CreateScope();
            var produtorRepository = scope.ServiceProvider.GetService<IProdutorRepository>();
            if (produtorRepository == null)
            {
                return null;
            }

            var produtor = await produtorRepository.GetByIdAsync(produtorId.Value);
            if (produtor == null)
            {
                return null;
            }

            var cpf = Regex.Replace(produtor.Cpf ?? string.Empty, "\\D", string.Empty);
            if (cpf.Length != 11)
            {
                return null;
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoLCPR",
                "consultas-nfe",
                cpf);
        }

        private async Task<SefazNFeDetalheCapturado?> ExtrairDetalhesNotaAsync(string chaveAcesso, string? identificadorDetalhe, string? diretorioCache)
        {
            if (SefazWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(chaveAcesso))
            {
                return null;
            }

            var detalheCache = TentarLerDetalheDoCache(diretorioCache, chaveAcesso);
            if (detalheCache != null)
            {
                return detalheCache;
            }

            TaskCompletionSource<string?> detalheUrlTcs;
            lock (_capturaEventoLock)
            {
                _proximoDetalheUrlTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                detalheUrlTcs = _proximoDetalheUrlTcs;
            }

            var script = $$"""
                (async () => {
                    const key = {{JsonSerializer.Serialize(chaveAcesso)}};
                    const detailId = {{JsonSerializer.Serialize(identificadorDetalhe)}};
                    const invokeValue = detailId && String(detailId).trim().length > 0 ? String(detailId).trim() : key;
                    let detalheUrl = null;

                    const setDetalheUrl = (value, baseHref) => {
                        if (!value || detalheUrl) {
                            return;
                        }

                        try {
                            detalheUrl = new URL(String(value), baseHref || window.location.href).href;
                        } catch (_) {
                            detalheUrl = String(value);
                        }
                    };

                    try {
                        const fn = window.visualizarNfe || window.visualizarNFe || window.visualizarDnfe || window.visualizarDNFe;
                        if (typeof fn === 'function') {
                            try {
                                fn(invokeValue);
                            } catch (_) {
                                try {
                                    fn(key);
                                } catch (_) {
                                }
                            }
                            await new Promise(resolve => setTimeout(resolve, 1800));
                        } else {
                            const allFrames = Array.from(document.querySelectorAll('iframe'));
                            for (const frame of allFrames) {
                                try {
                                    const w = frame.contentWindow;
                                    if (!w) {
                                        continue;
                                    }

                                    const frameFn = w.visualizarNfe || w.visualizarNFe || w.visualizarDnfe || w.visualizarDNFe;
                                    if (typeof frameFn === 'function') {
                                        try {
                                            frameFn(invokeValue);
                                        } catch (_) {
                                            frameFn(key);
                                        }
                                        await new Promise(resolve => setTimeout(resolve, 1800));
                                        break;
                                    }
                                } catch (_) {
                                }
                            }
                        }

                        if (!detalheUrl) {
                            const endpoint = window.location.pathname + '/visualizarDnfe';
                            const payloads = [
                                { id: invokeValue },
                                { codigo: invokeValue },
                                { chaveAcesso: key },
                                { chave: key },
                                { chaveNfe: key },
                                { id: key },
                                key
                            ];

                            for (const payload of payloads) {
                                try {
                                    const response = await fetch(endpoint, {
                                        method: 'POST',
                                        headers: { 'Content-Type': 'application/json; charset=utf-8' },
                                        credentials: 'include',
                                        body: JSON.stringify(payload)
                                    });

                                    const txt = await response.text();
                                    const outer = JSON.parse(txt);
                                    const inner = outer && outer.d ? JSON.parse(outer.d) : null;
                                    const sucesso = !!(inner && (inner.Sucesso ?? inner.sucesso ?? inner.Success ?? inner.success));
                                    const result = inner && (inner.Result ?? inner.result ?? inner.Url ?? inner.url);
                                    if (sucesso && result) {
                                        setDetalheUrl(result, window.location.href);
                                        break;
                                    }
                                } catch (_) {
                                }
                            }
                        }

                        return {
                            sucesso: !!detalheUrl,
                            detalheUrl: detalheUrl
                        };
                    } catch (_) {
                        return {
                            sucesso: false,
                            detalheUrl: null
                        };
                    }
                })();
                """;

            var json = await SefazWebView.ExecuteScriptAsync(script);
            var timeoutDetalhe = Task.Delay(TimeSpan.FromSeconds(12));
            var completedDetalhe = await Task.WhenAny(detalheUrlTcs.Task, timeoutDetalhe);
            var detalheUrlRede = completedDetalhe == detalheUrlTcs.Task ? detalheUrlTcs.Task.Result : null;

            lock (_capturaEventoLock)
            {
                _proximoDetalheUrlTcs = null;
            }

            string? detalheUrlScript = null;
            if (!string.IsNullOrWhiteSpace(json) && !string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
            {
                var detalheScript = JsonSerializer.Deserialize<SefazNFeDetalheCapturado>(json, JsonCaseInsensitiveOptions);
                detalheUrlScript = detalheScript?.DetalheUrl;
            }

            var detalheUrl = !string.IsNullOrWhiteSpace(detalheUrlRede) ? detalheUrlRede : detalheUrlScript;
            if (string.IsNullOrWhiteSpace(detalheUrl))
            {
                detalheUrl = await ObterDetalheUrlViaHttpPostAsync(chaveAcesso, identificadorDetalhe);
            }

            if (string.IsNullOrWhiteSpace(detalheUrl))
            {
                return null;
            }

            var html = await BaixarHtmlDetalheAsync(detalheUrl);
            if (string.IsNullOrWhiteSpace(html))
            {
                html = await BaixarHtmlDetalheViaWebViewAsync(detalheUrl);
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var detalheExtraido = ExtrairDetalhesDeHtml(html);
            detalheExtraido.Sucesso = true;
            detalheExtraido.DetalheUrl = detalheUrl;
            detalheExtraido.HtmlConsulta = html;
            return detalheExtraido;
        }

        private async Task<string?> BaixarHtmlDetalheAsync(string detalheUrl)
        {
            if (SefazWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(detalheUrl))
            {
                return null;
            }

            try
            {
                var baseUri = SefazWebView.Source ?? new Uri(ImportarViewModel.SefazUrl);
                if (!Uri.TryCreate(baseUri, detalheUrl, out var detalheUri))
                {
                    return null;
                }

                var cookies = await SefazWebView.CoreWebView2.CookieManager.GetCookiesAsync(detalheUri.ToString());
                var cookieHeader = string.Join("; ", cookies.Select(item => $"{item.Name}={item.Value}"));

                using var httpClientHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using var httpClient = new HttpClient(httpClientHandler);
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
                }

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
                httpClient.DefaultRequestHeaders.Referrer = baseUri;
                return await httpClient.GetStringAsync(detalheUri);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> ObterDetalheUrlViaHttpPostAsync(string chaveAcesso, string? identificadorDetalhe)
        {
            if (SefazWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(chaveAcesso))
            {
                return null;
            }

            try
            {
                var baseUri = SefazWebView.Source ?? new Uri(ImportarViewModel.SefazUrl);
                var endpoint = new Uri(baseUri, "/NotaFiscalEletronica.aspx/visualizarDnfe");

                var cookies = await SefazWebView.CoreWebView2.CookieManager.GetCookiesAsync(endpoint.ToString());
                var cookieHeader = string.Join("; ", cookies.Select(item => $"{item.Name}={item.Value}"));

                using var httpClientHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using var httpClient = new HttpClient(httpClientHandler);
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
                }

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Referrer = baseUri;

                var payloads = new List<object>();

                if (!string.IsNullOrWhiteSpace(identificadorDetalhe))
                {
                    payloads.Add(new { id = identificadorDetalhe });
                    payloads.Add(new { codigo = identificadorDetalhe });
                }

                payloads.Add(new { chaveAcesso });
                payloads.Add(new { chave = chaveAcesso });
                payloads.Add(new { chaveNfe = chaveAcesso });
                payloads.Add(new { codigo = chaveAcesso });
                payloads.Add(new { id = chaveAcesso });
                payloads.Add(chaveAcesso);

                foreach (var payload in payloads)
                {
                    try
                    {
                        var jsonBody = JsonSerializer.Serialize(payload);
                        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        using var response = await httpClient.PostAsync(endpoint, content);
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var detalheUrl = ExtrairDetalheUrlDaRespostaVisualizarDnfe(responseBody);
                        if (!string.IsNullOrWhiteSpace(detalheUrl))
                        {
                            return detalheUrl;
                        }
                    }
                    catch
                    {
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> BaixarHtmlDetalheViaWebViewAsync(string detalheUrl)
        {
            if (SefazWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(detalheUrl))
            {
                return null;
            }

            try
            {
                var script = $$"""
                    (async () => {
                        try {
                            const target = {{JsonSerializer.Serialize(detalheUrl)}};
                            const response = await fetch(target, {
                                method: 'GET',
                                credentials: 'include'
                            });

                            if (!response || !response.ok) {
                                return { sucesso: false, html: null };
                            }

                            const html = await response.text();
                            return { sucesso: !!html, html };
                        } catch (_) {
                            return { sucesso: false, html: null };
                        }
                    })();
                    """;

                var json = await SefazWebView.ExecuteScriptAsync(script);
                if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var resultado = JsonSerializer.Deserialize<SefazHtmlFetchResult>(json, JsonCaseInsensitiveOptions);
                if (resultado == null || !resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Html))
                {
                    return null;
                }

                return resultado.Html;
            }
            catch
            {
                return null;
            }
        }

        private static SefazNFeDetalheCapturado? TentarLerDetalheDoCache(string? diretorioCache, string chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(diretorioCache) || !Directory.Exists(diretorioCache))
            {
                return null;
            }

            var filePath = Path.Combine(diretorioCache, $"{chaveAcesso}.html");
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var html = File.ReadAllText(filePath);
                return ExtrairDetalhesDeHtml(html);
            }
            catch
            {
                return null;
            }
        }

        private static SefazNFeDetalheCapturado ExtrairDetalhesDeHtml(string html)
        {
            var texto = Regex.Replace(html, "<script[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, "<style[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, "<[^>]+>", " ");
            texto = WebUtility.HtmlDecode(texto);
            texto = Regex.Replace(texto, "\\s+", " ").Trim();

            string? natureza = null;
            var naturezaMatch = Regex.Match(texto, "Natureza\\s+da\\s+Opera[cç][aã]o\\s*[:\\-]?\\s*([^\\.;]+)", RegexOptions.IgnoreCase);
            if (naturezaMatch.Success)
            {
                natureza = naturezaMatch.Groups[1].Value.Trim();
            }

            var cfops = new List<string>();
            foreach (Match match in Regex.Matches(texto, "CFOP\\s*[:\\-]?\\s*(\\d{4})", RegexOptions.IgnoreCase))
            {
                var cfop = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(cfop) && !cfops.Contains(cfop))
                {
                    cfops.Add(cfop);
                }
            }

            if (cfops.Count == 0)
            {
                foreach (Match match in Regex.Matches(texto, "\\b[1-7]\\d{3}\\b"))
                {
                    var cfop = match.Value.Trim();
                    if (!cfops.Contains(cfop))
                    {
                        cfops.Add(cfop);
                    }
                }
            }

            return new SefazNFeDetalheCapturado
            {
                Sucesso = true,
                NaturezaOperacao = natureza,
                Cfops = cfops,
                DescricoesProdutosServicos = new List<string>(),
                HtmlConsulta = html
            };
        }

        private async Task<GridPaginacao?> ObterPaginacaoGridAsync()
        {
            if (SefazWebView.CoreWebView2 == null)
            {
                return null;
            }

            const string script = @"
                (() => {
                    try {
                        if (!window.jQuery) {
                            return null;
                        }

                        const grid = window.jQuery('#jqGridTableDIVGrid');
                        if (!grid || grid.length === 0 || typeof grid.jqGrid !== 'function') {
                            return null;
                        }

                        return {
                            page: Number(grid.jqGrid('getGridParam', 'page') || 1),
                            lastPage: Number(grid.jqGrid('getGridParam', 'lastpage') || 1),
                            records: Number(grid.jqGrid('getGridParam', 'records') || 0),
                            rowNum: Number(grid.jqGrid('getGridParam', 'rowNum') || 0)
                        };
                    } catch (_) {
                        return null;
                    }
                })();";

            var json = await SefazWebView.ExecuteScriptAsync(script);
            if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = doc.RootElement;
            return new GridPaginacao
            {
                Page = root.TryGetProperty("page", out var pageElement) ? pageElement.GetInt32() : 1,
                LastPage = root.TryGetProperty("lastPage", out var lastPageElement) ? lastPageElement.GetInt32() : 1,
                Records = root.TryGetProperty("records", out var recordsElement) ? recordsElement.GetInt32() : 0,
                RowNum = root.TryGetProperty("rowNum", out var rowNumElement) ? rowNumElement.GetInt32() : 0
            };
        }

        private async Task<bool> RecarregarPaginaGridAsync(int pagina)
        {
            if (SefazWebView.CoreWebView2 == null)
            {
                return false;
            }

            TaskCompletionSource<bool> tcs;
            lock (_capturaEventoLock)
            {
                _proximaCapturaTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                tcs = _proximaCapturaTcs;
            }

            var script = $@"
                (() => {{
                    try {{
                        if (!window.jQuery) {{
                            return false;
                        }}

                        const grid = window.jQuery('#jqGridTableDIVGrid');
                        if (!grid || grid.length === 0 || typeof grid.jqGrid !== 'function') {{
                            return false;
                        }}

                        grid.jqGrid('setGridParam', {{ page: {pagina} }});
                        grid.trigger('reloadGrid');
                        return true;
                    }} catch (_) {{
                        return false;
                    }}
                }})();";

            var json = await SefazWebView.ExecuteScriptAsync(script);
            var disparouRequisicao = string.Equals(json, "true", StringComparison.OrdinalIgnoreCase);
            if (!disparouRequisicao)
            {
                lock (_capturaEventoLock)
                {
                    _proximaCapturaTcs = null;
                }

                return false;
            }

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(12));
            var completed = await Task.WhenAny(tcs.Task, timeoutTask);

            lock (_capturaEventoLock)
            {
                _proximaCapturaTcs = null;
            }

            return completed == tcs.Task;
        }

        private async Task<IReadOnlyList<string>> CapturarChavesDaPaginaAsync()
        {
            if (SefazWebView.CoreWebView2 == null)
            {
                return Array.Empty<string>();
            }

            const string script = @"
                (() => {
                    const textos = [];
                    const corpo = (document.body && document.body.innerText) ? document.body.innerText : '';
                    if (corpo) textos.push(corpo);

                    const iframes = Array.from(document.querySelectorAll('iframe'));
                    for (const frame of iframes) {
                        try {
                            const doc = frame.contentDocument || (frame.contentWindow && frame.contentWindow.document);
                            const txt = doc && doc.body ? doc.body.innerText : '';
                            if (txt) textos.push(txt);
                        } catch (_) {
                        }
                    }

                    return textos;
                })();";

            var json = await SefazWebView.ExecuteScriptAsync(script);
            var valores = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

            var resultado = new List<string>();
            foreach (var valor in valores)
            {
                var matches = ChaveRegex.Matches(valor);
                foreach (Match match in matches)
                {
                    if (!resultado.Contains(match.Value))
                    {
                        resultado.Add(match.Value);
                    }
                }
            }

            return resultado;
        }

        private static List<SefazNFeCapturada> ExtrairNotasDaRespostaCarregarLista(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new List<SefazNFeCapturada>();
            }

            using var rootDocument = JsonDocument.Parse(responseBody);
            if (!rootDocument.RootElement.TryGetProperty("d", out var dElement) || dElement.ValueKind == JsonValueKind.Null)
            {
                return new List<SefazNFeCapturada>();
            }

            var payload = dElement.ValueKind == JsonValueKind.String
                ? dElement.GetString()
                : dElement.GetRawText();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<SefazNFeCapturada>();
            }

            using var gridDocument = JsonDocument.Parse(payload);
            if (!gridDocument.RootElement.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            {
                return new List<SefazNFeCapturada>();
            }

            var notas = new List<SefazNFeCapturada>();
            foreach (var row in rowsElement.EnumerateArray())
            {
                var nota = ConverterLinhaEmNFe(row);
                if (nota != null)
                {
                    notas.Add(nota);
                }
            }

            return notas;
        }

        private static SefazNFeCapturada? ConverterLinhaEmNFe(JsonElement row)
        {
            if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty("cell", out var cellElement) && cellElement.ValueKind == JsonValueKind.Array)
            {
                var celulas = cellElement.EnumerateArray().Select(ExtrairTexto).ToList();
                var identificadorDetalhe = ObterPropriedadeTexto(row, "id")
                    ?? ObterPropriedadeTexto(row, "Id")
                    ?? ObterPropriedadeTexto(row, "codigo")
                    ?? ObterPropriedadeTexto(row, "Codigo");
                return CriarNotaPorIndice(celulas, identificadorDetalhe);
            }

            if (row.ValueKind == JsonValueKind.Array)
            {
                var celulas = row.EnumerateArray().Select(ExtrairTexto).ToList();
                return CriarNotaPorIndice(celulas, null);
            }

            if (row.ValueKind == JsonValueKind.Object)
            {
                return CriarNotaPorObjeto(row);
            }

            return null;
        }

        private static SefazNFeCapturada? CriarNotaPorIndice(IReadOnlyList<string> celulas, string? identificadorDetalhe)
        {
            if (celulas.Count < 14)
            {
                return null;
            }

            var chave = celulas.ElementAtOrDefault(2)?.Trim();
            if (string.IsNullOrWhiteSpace(chave) || !ChaveRegex.IsMatch(chave))
            {
                return null;
            }

            return new SefazNFeCapturada
            {
                ChaveAcesso = chave,
                IdentificadorDetalhe = LimparTexto(identificadorDetalhe),
                CpfCnpjDestinatario = LimparTexto(celulas.ElementAtOrDefault(5)),
                IeDestinatario = LimparTexto(celulas.ElementAtOrDefault(3)),
                RazaoSocialDestinatario = LimparTexto(celulas.ElementAtOrDefault(4)),
                CpfCnpjEmitente = LimparTexto(celulas.ElementAtOrDefault(8)),
                IeEmitente = LimparTexto(celulas.ElementAtOrDefault(6)),
                RazaoSocialEmitente = LimparTexto(celulas.ElementAtOrDefault(7)),
                NumeroNota = LimparTexto(celulas.ElementAtOrDefault(9)),
                DataEmissao = TentarConverterData(celulas.ElementAtOrDefault(10)),
                ValorTotal = TentarConverterDecimal(celulas.ElementAtOrDefault(13)),
                Situacao = LimparTexto(celulas.ElementAtOrDefault(16))
            };
        }

        private static SefazNFeCapturada? CriarNotaPorObjeto(JsonElement row)
        {
            var chave = ObterPropriedadeTexto(row, "nfe_chave_acesso");
            if (string.IsNullOrWhiteSpace(chave) || !ChaveRegex.IsMatch(chave))
            {
                return null;
            }

            return new SefazNFeCapturada
            {
                ChaveAcesso = chave,
                IdentificadorDetalhe = ObterPropriedadeTexto(row, "id")
                    ?? ObterPropriedadeTexto(row, "Id")
                    ?? ObterPropriedadeTexto(row, "codigo")
                    ?? ObterPropriedadeTexto(row, "Codigo"),
                CpfCnpjDestinatario = ObterPropriedadeTexto(row, "nfe_cnpj_cpf_destinatario"),
                IeDestinatario = ObterPropriedadeTexto(row, "nfe_ie_destinatario"),
                RazaoSocialDestinatario = ObterPropriedadeTexto(row, "nfe_razao_social_destinatario"),
                CpfCnpjEmitente = ObterPropriedadeTexto(row, "nfe_cnpj_cpf_emitente"),
                IeEmitente = ObterPropriedadeTexto(row, "nfe_ie_emitente"),
                RazaoSocialEmitente = ObterPropriedadeTexto(row, "nfe_razao_social_emitente"),
                NumeroNota = ObterPropriedadeTexto(row, "nfe_numero"),
                DataEmissao = TentarConverterData(ObterPropriedadeTexto(row, "nfe_data_emissao")),
                ValorTotal = TentarConverterDecimal(ObterPropriedadeTexto(row, "nfe_vlr_total")),
                Situacao = ObterPropriedadeTexto(row, "nfe_situacao")
            };
        }

        private static string ExtrairTexto(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.Null => string.Empty,
                _ => element.GetRawText()
            };
        }

        private static string? ObterPropriedadeTexto(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return LimparTexto(ExtrairTexto(value));
        }

        private static string? LimparTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Trim();
        }

        private static DateTime? TentarConverterData(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var formatos = new[]
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy"
            };

            if (DateTime.TryParseExact(valor.Trim(), formatos, new CultureInfo("pt-BR"), DateTimeStyles.None, out var data))
            {
                return data;
            }

            if (DateTime.TryParse(valor, new CultureInfo("pt-BR"), DateTimeStyles.None, out data))
            {
                return data;
            }

            return null;
        }

        private static decimal TentarConverterDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return 0m;
            }

            var texto = valor
                .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty)
                .Trim();

            if (decimal.TryParse(texto, NumberStyles.Any, new CultureInfo("pt-BR"), out var decimalPtBr))
            {
                return decimalPtBr;
            }

            if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalInvariant))
            {
                return decimalInvariant;
            }

            return 0m;
        }

        private sealed class GridPaginacao
        {
            public int Page { get; set; }
            public int LastPage { get; set; }
            public int Records { get; set; }
            public int RowNum { get; set; }
        }

        private sealed class SefazNFeDetalheCapturado
        {
            public bool Sucesso { get; set; }
            public string? DetalheUrl { get; set; }
            public string? NaturezaOperacao { get; set; }
            public List<string> DescricoesProdutosServicos { get; set; } = new();
            public List<string> Cfops { get; set; } = new();
            public string? HtmlConsulta { get; set; }
        }

        private sealed class SefazHtmlFetchResult
        {
            public bool Sucesso { get; set; }
            public string? Html { get; set; }
        }
    }
}
