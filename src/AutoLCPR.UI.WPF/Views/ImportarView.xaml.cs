using AutoLCPR.UI.WPF.ViewModels;
using Microsoft.Web.WebView2.Core;
using System.Windows.Controls;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoLCPR.UI.WPF.Views
{
    /// <summary>
    /// Interação lógica para ImportarView.xaml
    /// </summary>
    public partial class ImportarView : UserControl
    {
        private static readonly Regex ChaveRegex = new("\\d{44}", RegexOptions.Compiled);

        public ImportarView()
        {
            InitializeComponent();
            DataContext = new ImportarViewModel();
            Loaded += ImportarView_Loaded;
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
                SefazWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                viewModel.CapturarChavesNoNavegadorAsync = CapturarChavesDaPaginaAsync;
                viewModel.Status = "Navegador interno pronto. Faça login na SEFAZ no painel ao lado.";
            }
            catch (Exception ex)
            {
                viewModel.Status = $"Falha ao inicializar navegador interno: {ex.Message}";
            }
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
            if (SefazWebView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(e.Uri))
            {
                SefazWebView.CoreWebView2.Navigate(e.Uri);
                e.Handled = true;
            }
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
    }
}
