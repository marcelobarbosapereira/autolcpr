using System.Windows;
using System.Windows.Input;

namespace AutoLCPR.UI.WPF.Views
{
    public partial class ConfirmacaoExclusaoWindow : Window
    {
        public ConfirmacaoExclusaoWindow(string nomeProduto, string tipoItem = "produtor")
        {
            InitializeComponent();
            ProdutorNameTextBlock.Text = nomeProduto;
            
            // Adaptar mensagem baseado no tipo de item
            string mensagem = tipoItem.ToLower() switch
            {
                "despesa" => "Deseja realmente EXCLUIR a despesa selecionada?",
                "receita" => "Deseja realmente EXCLUIR a receita selecionada?",
                "propriedade" => "Deseja realmente EXCLUIR a propriedade selecionada?",
                _ => "Deseja realmente EXCLUIR o item selecionado?"
            };
            
            MensagemTextBlock.Text = mensagem;
        }

        private void NaoClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SimClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            this.DialogResult = false;
            this.Close();
        }
    }
}
