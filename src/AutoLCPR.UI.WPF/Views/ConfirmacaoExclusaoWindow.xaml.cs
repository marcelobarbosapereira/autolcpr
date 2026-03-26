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
            var multiplosItens = nomeProduto.Contains("selecionad");
            
            // Adaptar mensagem baseado no tipo de item
            string mensagem = (tipoItem.ToLower(), multiplosItens) switch
            {
                ("despesa", true) => "Deseja realmente EXCLUIR as despesas selecionadas?",
                ("despesa", false) => "Deseja realmente EXCLUIR a despesa selecionada?",
                ("receita", true) => "Deseja realmente EXCLUIR as receitas selecionadas?",
                ("receita", false) => "Deseja realmente EXCLUIR a receita selecionada?",
                ("propriedade", true) => "Deseja realmente EXCLUIR as propriedades selecionadas?",
                ("propriedade", false) => "Deseja realmente EXCLUIR a propriedade selecionada?",
                (_, true) => "Deseja realmente EXCLUIR os itens selecionados?",
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

        private void FecharClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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
