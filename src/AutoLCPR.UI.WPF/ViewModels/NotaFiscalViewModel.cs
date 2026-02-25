using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para cadastro de notas fiscais
    /// </summary>
    public class NotaFiscalViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private int _id = 0;
        private bool _isEditMode = false;
        private string? _chaveAcesso;
        private string _numeroDaNota = string.Empty;
        private decimal _valorNotaFiscal = 0m;
        private DateTime _dataEmissao = DateTime.Now;
        private string _origem = string.Empty;
        private string _destino = string.Empty;
        private string _descricao = string.Empty;
        private TipoNota _tipoNota = TipoNota.Entrada;
        private string _produtorNome = string.Empty;
        private int _produtorId = 0;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (_isEditMode != value)
                {
                    _isEditMode = value;
                    OnPropertyChanged(nameof(IsEditMode));
                }
            }
        }

        public Action? FecharJanela { get; set; }
        public Action? AtualizarDashboard { get; set; }

        public string? ChaveAcesso
        {
            get => _chaveAcesso;
            set
            {
                if (_chaveAcesso != value)
                {
                    _chaveAcesso = value;
                    OnPropertyChanged(nameof(ChaveAcesso));
                }
            }
        }

        public string NumeroDaNota
        {
            get => _numeroDaNota;
            set
            {
                if (_numeroDaNota != value)
                {
                    _numeroDaNota = value;
                    OnPropertyChanged(nameof(NumeroDaNota));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public decimal ValorNotaFiscal
        {
            get => _valorNotaFiscal;
            set
            {
                if (_valorNotaFiscal != value)
                {
                    _valorNotaFiscal = value;
                    OnPropertyChanged(nameof(ValorNotaFiscal));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public DateTime DataEmissao
        {
            get => _dataEmissao;
            set
            {
                if (_dataEmissao != value)
                {
                    _dataEmissao = value;
                    OnPropertyChanged(nameof(DataEmissao));
                }
            }
        }

        public string Origem
        {
            get => _origem;
            set
            {
                if (_origem != value)
                {
                    _origem = value;
                    OnPropertyChanged(nameof(Origem));
                    OnPropertyChanged(nameof(OrigemDestinoValor));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Destino
        {
            get => _destino;
            set
            {
                if (_destino != value)
                {
                    _destino = value;
                    OnPropertyChanged(nameof(Destino));
                    OnPropertyChanged(nameof(OrigemDestinoValor));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Descricao
        {
            get => _descricao;
            set
            {
                if (_descricao != value)
                {
                    _descricao = value;
                    OnPropertyChanged(nameof(Descricao));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public TipoNota TipoNota
        {
            get => _tipoNota;
            set
            {
                if (_tipoNota != value)
                {
                    _tipoNota = value;
                    OnPropertyChanged(nameof(TipoNota));
                    OnPropertyChanged(nameof(TipoNotaDisplay));
                    OnPropertyChanged(nameof(EhReceita));
                    OnPropertyChanged(nameof(EhDespesa));
                    OnPropertyChanged(nameof(OrigemDestinoLabel));
                    OnPropertyChanged(nameof(OrigemDestinoValor));
                    
                    // Preencher automaticamente Origem ou Destino baseado no tipo
                    if (_tipoNota == TipoNota.Saida)
                    {
                        Origem = _produtorNome;
                        Destino = string.Empty;
                    }
                    else
                    {
                        Destino = _produtorNome;
                        Origem = string.Empty;
                    }
                    
                    // Notificar mudanças para que o comando atualize
                    OnPropertyChanged(nameof(Origem));
                    OnPropertyChanged(nameof(Destino));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string TipoNotaDisplay
        {
            get => _tipoNota == TipoNota.Entrada ? "Entrada" : "Saída";
        }

        public bool EhReceita => _tipoNota == TipoNota.Saida;
        public bool EhDespesa => _tipoNota == TipoNota.Entrada;

        public string OrigemDestinoLabel
        {
            get => _tipoNota == TipoNota.Saida ? "Destino *" : "Origem *";
        }

        public string OrigemDestinoValor
        {
            get => _tipoNota == TipoNota.Saida ? Destino : Origem;
            set
            {
                if (_tipoNota == TipoNota.Saida)
                {
                    Destino = value;
                }
                else
                {
                    Origem = value;
                }
            }
        }

        public string ProdutorNome
        {
            get => _produtorNome;
            set
            {
                if (_produtorNome != value)
                {
                    _produtorNome = value;
                    // Preencher Origem ou Destino quando produtor é definido
                    if (_tipoNota == TipoNota.Saida)
                    {
                        Origem = _produtorNome;
                    }
                    else
                    {
                        Destino = _produtorNome;
                    }
                    
                    // Notificar mudanças para que o comando atualize
                    OnPropertyChanged(nameof(Origem));
                    OnPropertyChanged(nameof(Destino));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int ProdutorId
        {
            get => _produtorId;
            set => _produtorId = value;
        }

        public List<string> TiposNota { get; } = new List<string> { "Entrada", "Saída" };

        public ICommand SalvarCommand { get; }
        public ICommand LimparCommand { get; }

        public NotaFiscalViewModel()
        {
            _serviceProvider = (Application.Current as App)?.ServiceProvider;
            SalvarCommand = new RelayCommand(async () => await SalvarAsync(), CanSalvar);
            LimparCommand = new RelayCommand(Limpar);
        }

        private async Task SalvarAsync()
        {
            if (!ValidarCampos())
                return;

            if (_serviceProvider == null)
            {
                AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notaRepo = scope.ServiceProvider.GetRequiredService<INotaFiscalRepository>();

                if (IsEditMode)
                {
                    // Modo edição - atualizar nota existente
                    var notaExistente = await notaRepo.GetByIdAsync(_id);
                    if (notaExistente == null)
                    {
                        AlertService.Show("Nota fiscal não encontrada.", "Erro", AlertType.Error);
                        return;
                    }

                    // Atualizar campos
                    notaExistente.ChaveAcesso = _chaveAcesso;
                    notaExistente.DataEmissao = _dataEmissao;
                    notaExistente.NumeroDaNota = _numeroDaNota;
                    notaExistente.ValorNotaFiscal = _valorNotaFiscal;
                    notaExistente.Origem = _origem;
                    notaExistente.Destino = _destino;
                    notaExistente.Descricao = _descricao;
                    notaExistente.TipoNota = _tipoNota;
                    notaExistente.ProdutorId = _produtorId;

                    await notaRepo.UpdateAsync(notaExistente);
                    AlertService.Show($"Nota Fiscal '{NumeroDaNota}' atualizada com sucesso!", "Sucesso", AlertType.Success);
                }
                else
                {
                    // Modo criação - inserir nova nota
                    var novaNota = new NotaFiscal
                    {
                        ChaveAcesso = _chaveAcesso,
                        DataEmissao = _dataEmissao,
                        NumeroDaNota = _numeroDaNota,
                        ValorNotaFiscal = _valorNotaFiscal,
                        Origem = _origem,
                        Destino = _destino,
                        Descricao = _descricao,
                        TipoNota = _tipoNota,
                        ProdutorId = _produtorId
                    };

                    await notaRepo.AddAsync(novaNota);
                    AlertService.Show($"Nota Fiscal '{NumeroDaNota}' cadastrada com sucesso!", "Sucesso", AlertType.Success);
                }

                AtualizarDashboard?.Invoke();
                FecharJanela?.Invoke();
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao salvar nota fiscal: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private bool CanSalvar()
        {
            // Validação baseada no tipo de nota
            bool origemDestinoValidos = _tipoNota == TipoNota.Entrada
                ? !string.IsNullOrWhiteSpace(Origem)  // Para despesa (Entrada): Origem deve estar preenchido
                : !string.IsNullOrWhiteSpace(Destino); // Para receita (Saida): Destino deve estar preenchido
            
            return !string.IsNullOrWhiteSpace(NumeroDaNota) &&
                   ValorNotaFiscal > 0 &&
                   origemDestinoValidos &&
                   !string.IsNullOrWhiteSpace(Descricao);
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroDaNota))
            {
                AlertService.Show("Por favor, preencha o numero da nota fiscal.", "Validacao", AlertType.Warning);
                return false;
            }

            if (ValorNotaFiscal <= 0)
            {
                AlertService.Show("Por favor, preencha um valor valido.", "Validacao", AlertType.Warning);
                return false;
            }

            // Validação baseada no tipo de nota
            if (_tipoNota == TipoNota.Entrada)
            {
                if (string.IsNullOrWhiteSpace(Origem))
                {
                    AlertService.Show("Por favor, preencha a origem.", "Validacao", AlertType.Warning);
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Destino))
                {
                    AlertService.Show("Por favor, preencha o destino.", "Validacao", AlertType.Warning);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(Descricao))
            {
                AlertService.Show("Por favor, preencha a descrição.", "Validacao", AlertType.Warning);
                return false;
            }

            return true;
        }

        private void Limpar()
        {
            ChaveAcesso = null;
            NumeroDaNota = string.Empty;
            ValorNotaFiscal = 0m;
            DataEmissao = DateTime.Now;
            Origem = string.Empty;
            Destino = string.Empty;
            Descricao = string.Empty;
            TipoNota = TipoNota.Entrada;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
