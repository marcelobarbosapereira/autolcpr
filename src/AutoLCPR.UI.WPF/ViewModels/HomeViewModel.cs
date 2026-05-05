using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.Infrastructure.Data;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AutoLCPR.UI.WPF.Commands;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para a tela inicial (Home) - Dashboard
    /// </summary>
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly ImportacaoContextoService? _importacaoContextoService;
        private int _totalRebanhos = 0;
        private int _notasFiscaisMes = 0;
        private decimal _saldoFinanceiro = 0m;
        private decimal _receitasMes = 0m;
        private decimal _despesasMes = 0m;
        private string _filtroSelecionado = "Despesas";
        private string _textoNovoButton = "+ Nova Despesa";
        private string _textoBusca = string.Empty;
        private Produtor? _produtorSelecionado;
        private Rebanho? _rebanhoSelecionado;
        private NotaFiscal? _notaFiscalSelecionada;
        private readonly ICollectionView _despesasView;
        private readonly ICollectionView _receitasView;
        private readonly ICollectionView _rebanhosView;

        public ObservableCollection<Produtor> Produtores { get; } = new();
        public ObservableCollection<NotaFiscal> Despesas { get; } = new();
        public ObservableCollection<NotaFiscal> Receitas { get; } = new();
        public ObservableCollection<Rebanho> Rebanhos { get; } = new();
        public ObservableCollection<NotaFiscal> NotasSelecionadas { get; } = new();

        public int TotalRebanhos
        {
            get => _totalRebanhos;
            set
            {
                if (_totalRebanhos != value)
                {
                    _totalRebanhos = value;
                    OnPropertyChanged(nameof(TotalRebanhos));
                }
            }
        }

        public int NotasFiscaisMes
        {
            get => _notasFiscaisMes;
            set
            {
                if (_notasFiscaisMes != value)
                {
                    _notasFiscaisMes = value;
                    OnPropertyChanged(nameof(NotasFiscaisMes));
                }
            }
        }

        public decimal SaldoFinanceiro
        {
            get => _saldoFinanceiro;
            set
            {
                if (_saldoFinanceiro != value)
                {
                    _saldoFinanceiro = value;
                    OnPropertyChanged(nameof(SaldoFinanceiro));
                }
            }
        }

        public decimal ReceitasMes
        {
            get => _receitasMes;
            set
            {
                if (_receitasMes != value)
                {
                    _receitasMes = value;
                    OnPropertyChanged(nameof(ReceitasMes));
                }
            }
        }

        public decimal DespesasMes
        {
            get => _despesasMes;
            set
            {
                if (_despesasMes != value)
                {
                    _despesasMes = value;
                    OnPropertyChanged(nameof(DespesasMes));
                }
            }
        }

        public string FiltroSelecionado
        {
            get => _filtroSelecionado;
            set
            {
                if (_filtroSelecionado != value)
                {
                    _filtroSelecionado = value;
                    NotasSelecionadas.Clear();
                    NotaFiscalSelecionada = null;
                    AtualizarTextoButton();
                    OnPropertyChanged(nameof(FiltroSelecionado));
                }
            }
        }

        public string TextoNovoButton
        {
            get => _textoNovoButton;
            set
            {
                if (_textoNovoButton != value)
                {
                    _textoNovoButton = value;
                    OnPropertyChanged(nameof(TextoNovoButton));
                }
            }
        }

        public string TextoBusca
        {
            get => _textoBusca;
            set
            {
                if (_textoBusca != value)
                {
                    _textoBusca = value;
                    OnPropertyChanged(nameof(TextoBusca));
                    AtualizarFiltrosBusca();
                }
            }
        }

        public ICommand SelecionarReceitasCommand { get; }
        public ICommand SelecionarDespesasCommand { get; }
        public ICommand SelecionarRebanhoCommand { get; }
        public ICommand AdicionarProdutorCommand { get; }
        public ICommand EditarProdutorCommand { get; }
        public ICommand ExcluirProdutorCommand { get; }
        public ICommand NovoItemCommand { get; }
        public ICommand ExcluirItemCommand { get; }
        public ICommand SelecionarRebanhoItemCommand { get; }
        public ICommand EditarRebanhoCommand { get; }
        public ICommand SelecionarNotaFiscalCommand { get; }
        public ICommand EditarNotaFiscalCommand { get; }

        public Produtor? ProdutorSelecionado
        {
            get => _produtorSelecionado;
            set
            {
                if (_produtorSelecionado != value)
                {
                    _produtorSelecionado = value;
                    if (_importacaoContextoService != null)
                    {
                        _importacaoContextoService.ProdutorSelecionadoId = value?.Id;
                        _importacaoContextoService.ProdutorSelecionadoNome = value?.Nome;
                    }
                    OnPropertyChanged(nameof(ProdutorSelecionado));
                    _ = LoadDadosProdutorAsync();
                }
            }
        }

        public Rebanho? RebanhoSelecionado
        {
            get => _rebanhoSelecionado;
            set
            {
                if (_rebanhoSelecionado != value)
                {
                    _rebanhoSelecionado = value;
                    OnPropertyChanged(nameof(RebanhoSelecionado));
                }
            }
        }

        public NotaFiscal? NotaFiscalSelecionada
        {
            get => _notaFiscalSelecionada;
            set
            {
                if (_notaFiscalSelecionada != value)
                {
                    _notaFiscalSelecionada = value;
                    OnPropertyChanged(nameof(NotaFiscalSelecionada));
                }
            }
        }

        public void DefinirNotasSelecionadas(System.Collections.Generic.IEnumerable<NotaFiscal> notas)
        {
            var notasLista = notas.ToList();

            NotasSelecionadas.Clear();
            foreach (var nota in notasLista)
            {
                NotasSelecionadas.Add(nota);
            }

            NotaFiscalSelecionada = notasLista.LastOrDefault();
        }

        /// <summary>
        /// Construtor
        /// </summary>
        public HomeViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            _importacaoContextoService = _serviceProvider?.GetService<ImportacaoContextoService>();

            _despesasView = CollectionViewSource.GetDefaultView(Despesas);
            _receitasView = CollectionViewSource.GetDefaultView(Receitas);
            _rebanhosView = CollectionViewSource.GetDefaultView(Rebanhos);

            _despesasView.Filter = FiltrarDespesa;
            _receitasView.Filter = FiltrarReceita;
            _rebanhosView.Filter = FiltrarRebanho;

            // Inicializar comandos
            SelecionarReceitasCommand = new RelayCommand(() => FiltroSelecionado = "Receitas");
            SelecionarDespesasCommand = new RelayCommand(() => FiltroSelecionado = "Despesas");
            SelecionarRebanhoCommand = new RelayCommand(() => FiltroSelecionado = "Rebanho");
            AdicionarProdutorCommand = new RelayCommand(AdicionarProdutor);
            EditarProdutorCommand = new RelayCommand(EditarProdutor);
            ExcluirProdutorCommand = new AsyncRelayCommand(ExcluirProdutor);
            NovoItemCommand = new RelayCommand(AbrirNovoItem);
            ExcluirItemCommand = new AsyncRelayCommand(ExcluirItem);
            SelecionarRebanhoItemCommand = new ParameterizedRelayCommand(SelecionarRebanhoItem);
            EditarRebanhoCommand = new ParameterizedRelayCommand(EditarRebanho);
            SelecionarNotaFiscalCommand = new ParameterizedRelayCommand(SelecionarNotaFiscal);
            EditarNotaFiscalCommand = new ParameterizedRelayCommand(EditarNotaFiscal);

            // Carregar dados
            ResetDashboard();
            _ = LoadProdutoresAsync();
        }

        private bool BuscaAtiva => !string.IsNullOrWhiteSpace(_textoBusca) && _textoBusca.Trim().Length >= 3;

        private void AtualizarFiltrosBusca()
        {
            _despesasView.Refresh();
            _receitasView.Refresh();
            _rebanhosView.Refresh();
        }

        private bool FiltrarDespesa(object item)
        {
            if (item is not NotaFiscal despesa)
            {
                return false;
            }

            return !BuscaAtiva || NotaCorrespondeBusca(despesa);
        }

        private bool FiltrarReceita(object item)
        {
            if (item is not NotaFiscal receita)
            {
                return false;
            }

            return !BuscaAtiva || NotaCorrespondeBusca(receita);
        }

        private bool FiltrarRebanho(object item)
        {
            if (item is not Rebanho rebanho)
            {
                return false;
            }

            if (!BuscaAtiva)
            {
                return true;
            }

            var termo = _textoBusca.Trim();

            return ContemTermo(rebanho.IdRebanho, termo)
                || ContemTermo(rebanho.NomeRebanho, termo)
                || ContemTermo(rebanho.Nascimentos.ToString(), termo)
                || ContemTermo(rebanho.Entradas.ToString(), termo)
                || ContemTermo(rebanho.Saidas.ToString(), termo)
                || ContemTermo(rebanho.Mortes.ToString(), termo);
        }

        private bool NotaCorrespondeBusca(NotaFiscal nota)
        {
            var termo = _textoBusca.Trim();

            return ContemTermo(nota.NumeroDaNota, termo)
                || ContemTermo(nota.Origem, termo)
                || ContemTermo(nota.Destino, termo)
                || ContemTermo(nota.Descricao, termo)
                || ContemTermo(nota.DataEmissao.ToString("dd/MM/yyyy"), termo)
                || ContemTermo(nota.ValorNotaFiscal.ToString("N2"), termo);
        }

        private static bool ContemTermo(string? texto, string termo)
        {
            return !string.IsNullOrWhiteSpace(texto)
                && texto.Contains(termo, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Abre a janela para adicionar um novo produtor
        /// </summary>
        private void AdicionarProdutor()
        {
            var view = new Views.ProdutorView();
            AbrirJanelaModal(view, "Adicionar Produtor");
            _ = LoadProdutoresAsync();
        }

        /// <summary>
        /// Abre a janela para editar o produtor selecionado
        /// </summary>
        private void EditarProdutor()
        {
            if (ProdutorSelecionado == null)
            {
                AlertService.Show("Selecione um produtor para editar.", "Atencao", AlertType.Warning);
                return;
            }

            var view = new Views.ProdutorView();
            if (view.DataContext is ProdutorViewModel viewModel)
            {
                viewModel.IsEditMode = true;
                viewModel.Id = ProdutorSelecionado.Id;
                viewModel.Nome = ProdutorSelecionado.Nome;
                viewModel.Cpf = ProdutorSelecionado.Cpf;
            }
            AbrirJanelaModal(view, "Editar Produtor");
            _ = LoadProdutoresAsync();
        }

        private void AbrirNovoItem()
        {
            switch (FiltroSelecionado)
            {
                case "Despesas":
                    {
                        var notaView = new Views.NotaFiscalView();
                        if (notaView.DataContext is NotaFiscalViewModel notaViewModel)
                        {
                            notaViewModel.TipoNota = TipoNota.Entrada;
                            if (ProdutorSelecionado != null)
                            {
                                notaViewModel.ProdutorNome = ProdutorSelecionado.Nome;
                                notaViewModel.ProdutorId = ProdutorSelecionado.Id;
                            }
                        }
                        AbrirJanelaModal(notaView, "Nova Despesa", notaView.DataContext as NotaFiscalViewModel);
                        break;
                    }
                case "Receitas":
                    {
                        var notaView = new Views.NotaFiscalView();
                        if (notaView.DataContext is NotaFiscalViewModel notaViewModel)
                        {
                            notaViewModel.TipoNota = TipoNota.Saida;
                            if (ProdutorSelecionado != null)
                            {
                                notaViewModel.ProdutorNome = ProdutorSelecionado.Nome;
                                notaViewModel.ProdutorId = ProdutorSelecionado.Id;
                            }
                        }
                        AbrirJanelaModal(notaView, "Nova Receita", notaView.DataContext as NotaFiscalViewModel);
                        break;
                    }
                case "Rebanho":
                    if (ProdutorSelecionado == null)
                    {
                        AlertService.Show("Selecione um produtor antes de cadastrar uma propriedade.", "Atencao", AlertType.Warning);
                        return;
                    }
                    var rebanhoView = new Views.RebanhoView();
                    if (rebanhoView.DataContext is RebanhoViewModel rebanhoViewModel)
                    {
                        rebanhoViewModel.ProdutorNome = ProdutorSelecionado.Nome;
                        rebanhoViewModel.ProdutorId = ProdutorSelecionado.Id;
                    }
                    AbrirJanelaModal(rebanhoView, "Novo Rebanho");
                    break;
                default:
                    AlertService.Show("Selecione um tipo valido antes de continuar.", "Atencao", AlertType.Warning);
                    break;
            }
        }

        private void AbrirJanelaModal(UserControl view, string title, NotaFiscalViewModel? notaViewModel = null)
        {
            var window = new System.Windows.Window
            {
                Content = view,
                Width = 850,
                Height = 750,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Title = title,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Owner = System.Windows.Application.Current.MainWindow,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 234, 237)),
                BorderThickness = new System.Windows.Thickness(1),
                WindowStyle = System.Windows.WindowStyle.None,
                ShowInTaskbar = false
            };
            
            // Configurar callbacks para NotaFiscalViewModel
            if (notaViewModel != null)
            {
                notaViewModel.FecharJanela = () => window.Close();
                notaViewModel.AtualizarDashboard = async () => await LoadDadosProdutorAsync();
            }
            
            // Configurar o callback para fechar a janela se o ViewModel for RebanhoViewModel
            if (view.DataContext is RebanhoViewModel rebanhoViewModel)
            {
                rebanhoViewModel.FecharJanela = () => window.Close();
                rebanhoViewModel.AtualizarDashboard = async () => await LoadDadosProdutorAsync();
            }
            
            window.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    window.Close();
                    e.Handled = true;
                }
            };
            window.ShowDialog();
        }

        /// <summary>
        /// Abre o diálogo para excluir um produtor selecionado
        /// </summary>
        private async Task ExcluirProdutor()
        {
            if (ProdutorSelecionado == null)
            {
                AlertService.Show("Selecione um produtor para excluir.", "Atencao", AlertType.Warning);
                return;
            }

            var dialog = new Views.ConfirmacaoExclusaoWindow(ProdutorSelecionado.Nome, "produtor");
            if (dialog.ShowDialog() == true)
            {
                if (_serviceProvider == null)
                {
                    AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                    return;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                    var produtorId = ProdutorSelecionado.Id;
                    await repo.DeleteAsync(produtorId);

                    var produtorPersistiu = await repo.GetByIdAsync(produtorId);
                    if (produtorPersistiu != null)
                    {
                        AlertService.Show("A exclusão do produtor não foi persistida no banco de dados.", "Erro", AlertType.Error);
                        return;
                    }

                    AlertService.Show("Produtor excluido com sucesso!", "Sucesso", AlertType.Success);
                    await LoadProdutoresAsync();
                }
                catch (Exception ex)
                {
                    AlertService.Show($"Erro ao excluir produtor: {ex.Message}", "Erro", AlertType.Error);
                }
            }
        }

        /// <summary>
        /// Seleciona um rebanho ao clicar uma vez
        /// </summary>
        private void SelecionarRebanhoItem(object? parameter)
        {
            if (parameter is Rebanho rebanho)
            {
                RebanhoSelecionado = rebanho;
            }
        }

        /// <summary>
        /// Abre a janela para editar um rebanho ao dar duplo clique
        /// </summary>
        private void EditarRebanho(object? parameter)
        {
            if (parameter is Rebanho rebanho)
            {
                if (ProdutorSelecionado == null)
                {
                    AlertService.Show("Selecione um produtor antes de editar uma propriedade.", "Atencao", AlertType.Warning);
                    return;
                }

                var rebanhoView = new Views.RebanhoView();
                if (rebanhoView.DataContext is RebanhoViewModel rebanhoViewModel)
                {
                    // Preencher os dados do rebanho selecionado
                    rebanhoViewModel.IsEditMode = true;
                    rebanhoViewModel.Id = rebanho.Id; // Chave primária do banco de dados
                    rebanhoViewModel.ProdutorNome = ProdutorSelecionado.Nome;
                    rebanhoViewModel.ProdutorId = ProdutorSelecionado.Id;
                    rebanhoViewModel.IdRebanho = rebanho.IdRebanho;
                    rebanhoViewModel.NomeRebanho = rebanho.NomeRebanho;
                    rebanhoViewModel.Mortes = rebanho.Mortes;
                    rebanhoViewModel.Nascimentos = rebanho.Nascimentos;
                    rebanhoViewModel.Entradas = rebanho.Entradas;
                    rebanhoViewModel.Saidas = rebanho.Saidas;
                }
                AbrirJanelaModal(rebanhoView, "Editar Propriedade");
            }
        }

        /// <summary>
        /// Seleciona uma nota fiscal ao clicar uma vez
        /// </summary>
        private void SelecionarNotaFiscal(object? parameter)
        {
            if (parameter is NotaFiscal nota)
            {
                NotaFiscalSelecionada = nota;
            }
        }

        /// <summary>
        /// Abre a janela para editar uma nota fiscal ao dar duplo clique
        /// </summary>
        private void EditarNotaFiscal(object? parameter)
        {
            if (parameter is NotaFiscal nota)
            {
                if (ProdutorSelecionado == null)
                {
                    AlertService.Show("Selecione um produtor antes de editar uma nota fiscal.", "Atencao", AlertType.Warning);
                    return;
                }

                var notaView = new Views.NotaFiscalView();
                if (notaView.DataContext is NotaFiscalViewModel notaViewModel)
                {
                    // Configurar modo de edição
                    notaViewModel.IsEditMode = true;
                    notaViewModel.Id = nota.Id; // Chave primária
                    
                    // Preencher os dados da nota selecionada
                    notaViewModel.TipoNota = nota.TipoNota;
                    notaViewModel.ProdutorNome = ProdutorSelecionado.Nome;
                    notaViewModel.ProdutorId = ProdutorSelecionado.Id;
                    notaViewModel.ChaveAcesso = nota.ChaveAcesso;
                    notaViewModel.DataEmissao = nota.DataEmissao;
                    notaViewModel.NumeroDaNota = nota.NumeroDaNota;
                    notaViewModel.ValorNotaFiscal = nota.ValorNotaFiscal;
                    notaViewModel.Origem = nota.Origem;
                    notaViewModel.Destino = nota.Destino;
                    notaViewModel.Descricao = nota.Descricao;
                }
                AbrirJanelaModal(notaView, "Editar Nota Fiscal", notaView.DataContext as NotaFiscalViewModel);
            }
        }

        /// <summary>
        /// Exclui o item selecionado baseado no filtro ativo
        /// </summary>
        private async Task ExcluirItem()
        {
            switch (FiltroSelecionado)
            {
                case "Despesas":
                    await ExcluirDespesa();
                    break;
                case "Receitas":
                    await ExcluirReceita();
                    break;
                case "Rebanho":
                    await ExcluirRebanho();
                    break;
                default:
                    AlertService.Show("Selecione um tipo valido antes de continuar.", "Atencao", AlertType.Warning);
                    break;
            }
        }

        /// <summary>
        /// Exclui a despesa (nota fiscal de entrada) selecionada
        /// </summary>
        private async Task ExcluirDespesa()
        {
            var notasParaExcluir = ObterNotasSelecionadas();

            if (notasParaExcluir.Count == 0)
            {
                AlertService.Show("Selecione ao menos uma despesa para excluir.", "Atencao", AlertType.Warning);
                return;
            }

            if (ProdutorSelecionado == null)
            {
                AlertService.Show("Nenhum produtor selecionado.", "Erro", AlertType.Error);
                return;
            }

            var descricao = notasParaExcluir.Count == 1
                ? $"despesa de {notasParaExcluir[0].DataEmissao:dd/MM/yyyy}"
                : $"{notasParaExcluir.Count} despesas selecionadas";

            var dialog = new Views.ConfirmacaoExclusaoWindow(descricao, "despesa");
            if (dialog.ShowDialog() == true)
            {
                if (_serviceProvider == null)
                {
                    AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                    return;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var idsExcluidos = notasParaExcluir.Select(item => item.Id).ToList();
                    foreach (var nota in notasParaExcluir)
                    {
                        await repo.DeleteAsync(nota.Id);
                        await ExcluirLancamentosLegadosDaNotaAsync(dbContext, nota);
                    }

                    var idsPersistentes = new List<int>();
                    foreach (var id in idsExcluidos)
                    {
                        if (await repo.GetByIdAsync(id) != null)
                        {
                            idsPersistentes.Add(id);
                        }
                    }

                    if (idsPersistentes.Count > 0)
                    {
                        AlertService.Show("Uma ou mais despesas não foram excluídas do banco de dados.", "Erro", AlertType.Error);
                        return;
                    }

                    var mensagem = notasParaExcluir.Count == 1
                        ? "Despesa excluida com sucesso!"
                        : $"{notasParaExcluir.Count} despesas excluidas com sucesso!";

                    AlertService.Show(mensagem, "Sucesso", AlertType.Success);
                    NotasSelecionadas.Clear();
                    NotaFiscalSelecionada = null;
                    await LoadDadosProdutorAsync();
                }
                catch (Exception ex)
                {
                    AlertService.Show($"Erro ao excluir despesa: {ex.Message}", "Erro", AlertType.Error);
                }
            }
        }

        /// <summary>
        /// Exclui a receita (nota fiscal de saida) selecionada
        /// </summary>
        private async Task ExcluirReceita()
        {
            var notasParaExcluir = ObterNotasSelecionadas();

            if (notasParaExcluir.Count == 0)
            {
                AlertService.Show("Selecione ao menos uma receita para excluir.", "Atencao", AlertType.Warning);
                return;
            }

            if (ProdutorSelecionado == null)
            {
                AlertService.Show("Nenhum produtor selecionado.", "Erro", AlertType.Error);
                return;
            }

            var descricao = notasParaExcluir.Count == 1
                ? $"receita de {notasParaExcluir[0].DataEmissao:dd/MM/yyyy}"
                : $"{notasParaExcluir.Count} receitas selecionadas";

            var dialog = new Views.ConfirmacaoExclusaoWindow(descricao, "receita");
            if (dialog.ShowDialog() == true)
            {
                if (_serviceProvider == null)
                {
                    AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                    return;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var idsExcluidos = notasParaExcluir.Select(item => item.Id).ToList();
                    foreach (var nota in notasParaExcluir)
                    {
                        await repo.DeleteAsync(nota.Id);
                        await ExcluirLancamentosLegadosDaNotaAsync(dbContext, nota);
                    }

                    var idsPersistentes = new List<int>();
                    foreach (var id in idsExcluidos)
                    {
                        if (await repo.GetByIdAsync(id) != null)
                        {
                            idsPersistentes.Add(id);
                        }
                    }

                    if (idsPersistentes.Count > 0)
                    {
                        AlertService.Show("Uma ou mais receitas não foram excluídas do banco de dados.", "Erro", AlertType.Error);
                        return;
                    }

                    var mensagem = notasParaExcluir.Count == 1
                        ? "Receita excluida com sucesso!"
                        : $"{notasParaExcluir.Count} receitas excluidas com sucesso!";

                    AlertService.Show(mensagem, "Sucesso", AlertType.Success);
                    NotasSelecionadas.Clear();
                    NotaFiscalSelecionada = null;
                    await LoadDadosProdutorAsync();
                }
                catch (Exception ex)
                {
                    AlertService.Show($"Erro ao excluir receita: {ex.Message}", "Erro", AlertType.Error);
                }
            }
        }

        /// <summary>
        /// Exclui o rebanho selecionado
        /// </summary>
        private async Task ExcluirRebanho()
        {
            if (RebanhoSelecionado == null)
            {
                AlertService.Show("Selecione uma propriedade para excluir.", "Atencao", AlertType.Warning);
                return;
            }

            if (ProdutorSelecionado == null)
            {
                AlertService.Show("Nenhum produtor selecionado.", "Erro", AlertType.Error);
                return;
            }

            var dialog = new Views.ConfirmacaoExclusaoWindow($"propriedade '{RebanhoSelecionado.NomeRebanho}'", "propriedade");
            if (dialog.ShowDialog() == true)
            {
                if (_serviceProvider == null)
                {
                    AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                    return;
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IRebanhoRepository>();
                    var rebanhoId = RebanhoSelecionado.Id;
                    await repo.DeleteAsync(rebanhoId);

                    var rebanhoPersistiu = await repo.GetByIdAsync(rebanhoId);
                    if (rebanhoPersistiu != null)
                    {
                        AlertService.Show("A exclusão da propriedade não foi persistida no banco de dados.", "Erro", AlertType.Error);
                        return;
                    }

                    AlertService.Show($"Propriedade '{RebanhoSelecionado.NomeRebanho}' excluida com sucesso!", "Sucesso", AlertType.Success);
                    RebanhoSelecionado = null;
                    await LoadDadosProdutorAsync();
                }
                catch (Exception ex)
                {
                    AlertService.Show($"Erro ao excluir propriedade: {ex.Message}", "Erro", AlertType.Error);
                }
            }
        }

        private System.Collections.Generic.List<NotaFiscal> ObterNotasSelecionadas()
        {
            if (NotasSelecionadas.Count > 0)
            {
                return NotasSelecionadas.ToList();
            }

            return NotaFiscalSelecionada != null
                ? new System.Collections.Generic.List<NotaFiscal> { NotaFiscalSelecionada }
                : new System.Collections.Generic.List<NotaFiscal>();
        }

        private static async Task ExcluirLancamentosLegadosDaNotaAsync(AppDbContext dbContext, NotaFiscal nota)
        {
            var tipoLancamento = nota.TipoNota == TipoNota.Saida ? TipoLancamento.Receita : TipoLancamento.Despesa;
            var diaInicio = nota.DataEmissao.Date;
            var diaFim = diaInicio.AddDays(1);

            // Primeiro tenta localizar por número da NF na descrição (mais preciso).
            var porNumeroNota = await dbContext.Lancamentos
                .Where(item => item.ProdutorId == nota.ProdutorId
                    && item.Tipo == tipoLancamento
                    && item.Data >= diaInicio
                    && item.Data < diaFim
                    && item.Descricao.Contains(nota.NumeroDaNota))
                .ToListAsync();

            if (porNumeroNota.Count > 0)
            {
                dbContext.Lancamentos.RemoveRange(porNumeroNota);
                await dbContext.SaveChangesAsync();
                return;
            }

            // Fallback legado: elimina lançamentos com mesma data/valor/tipo para o produtor.
            var porValorData = await dbContext.Lancamentos
                .Where(item => item.ProdutorId == nota.ProdutorId
                    && item.Tipo == tipoLancamento
                    && item.Data >= diaInicio
                    && item.Data < diaFim
                    && item.Valor == nota.ValorNotaFiscal)
                .ToListAsync();

            if (porValorData.Count > 0)
            {
                dbContext.Lancamentos.RemoveRange(porValorData);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task LoadProdutoresAsync()
        {
            if (_serviceProvider == null)
            {
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                var produtores = (await repo.GetAllAsync())
                    .OrderBy(p => p.Nome)
                    .ToList();

                Produtores.Clear();
                foreach (var produtor in produtores)
                {
                    Produtores.Add(produtor);
                }

                var produtorSelecionadoDoContexto = _importacaoContextoService?.ProdutorSelecionadoId;
                if (produtorSelecionadoDoContexto.HasValue)
                {
                    var produtorRestaurado = Produtores.FirstOrDefault(p => p.Id == produtorSelecionadoDoContexto.Value);
                    if (produtorRestaurado != null)
                    {
                        ProdutorSelecionado = produtorRestaurado;
                        return;
                    }
                }

                if (ProdutorSelecionado == null || !Produtores.Any(p => p.Id == ProdutorSelecionado.Id))
                {
                    ProdutorSelecionado = Produtores.FirstOrDefault();
                }
                else
                {
                    _ = LoadDadosProdutorAsync();
                }
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao carregar produtores: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        /// <summary>
        /// Reseta os dados do dashboard
        /// </summary>
        private void ResetDashboard()
        {
            TotalRebanhos = 0;
            NotasFiscaisMes = 0;
            SaldoFinanceiro = 0m;
            ReceitasMes = 0m;
            DespesasMes = 0m;
            NotasSelecionadas.Clear();
            NotaFiscalSelecionada = null;
            Despesas.Clear();
            Receitas.Clear();
            Rebanhos.Clear();
        }

        private async Task LoadDadosProdutorAsync()
        {
            if (_serviceProvider == null || ProdutorSelecionado == null)
            {
                ResetDashboard();
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notaRepo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();
                var rebanhoRepo = scope.ServiceProvider.GetRequiredService<IRebanhoRepository>();

                var notas = (await notaRepo.GetByProdutorIdAsync(ProdutorSelecionado.Id)).ToList();
                var rebanhos = (await rebanhoRepo.GetByProdutorIdAsync(ProdutorSelecionado.Id)).ToList();

                var despesas = notas
                    .Where(n => n.TipoNota == TipoNota.Entrada)
                    .OrderByDescending(n => n.DataEmissao)
                    .ToList();

                var receitas = notas
                    .Where(n => n.TipoNota == TipoNota.Saida)
                    .OrderByDescending(n => n.DataEmissao)
                    .ToList();

                Despesas.Clear();
                foreach (var despesa in despesas)
                {
                    Despesas.Add(despesa);
                }

                Receitas.Clear();
                foreach (var receita in receitas)
                {
                    Receitas.Add(receita);
                }

                Rebanhos.Clear();
                foreach (var rebanho in rebanhos)
                {
                    Rebanhos.Add(rebanho);
                }

                AtualizarFiltrosBusca();

                var now = DateTime.Now;
                var receitasMes = receitas
                    .Where(n => n.DataEmissao.Year == now.Year && n.DataEmissao.Month == now.Month)
                    .Sum(n => n.ValorNotaFiscal);
                var despesasMes = despesas
                    .Where(n => n.DataEmissao.Year == now.Year && n.DataEmissao.Month == now.Month)
                    .Sum(n => n.ValorNotaFiscal);

                TotalRebanhos = rebanhos.Count;
                NotasFiscaisMes = receitas.Count(n => n.DataEmissao.Year == now.Year && n.DataEmissao.Month == now.Month)
                    + despesas.Count(n => n.DataEmissao.Year == now.Year && n.DataEmissao.Month == now.Month);
                ReceitasMes = receitasMes;
                DespesasMes = despesasMes;
                SaldoFinanceiro = receitas.Sum(n => n.ValorNotaFiscal) - despesas.Sum(n => n.ValorNotaFiscal);
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao carregar dados do produtor: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        /// <summary>
        /// Atualiza o texto do botão "+ Novo..." baseado no filtro selecionado
        /// </summary>
        private void AtualizarTextoButton()
        {
            TextoNovoButton = FiltroSelecionado switch
            {
                "Receitas" => "+ Nova Receita",
                "Despesas" => "+ Nova Despesa",
                "Rebanho" => "+ Novo Rebanho",
                _ => "+ Novo Item"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

