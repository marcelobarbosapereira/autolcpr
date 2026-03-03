using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoLCPR.Domain.Entities;
using AutoLCPR.Domain.Repositories;
using AutoLCPR.UI.WPF.Commands;
using AutoLCPR.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLCPR.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel para cadastro de rebanhos
    /// </summary>
    public class RebanhoViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider? _serviceProvider;
        private int _id = 0;
        private string _idRebanho = string.Empty;
        private string _nomeRebanho = string.Empty;
        private int _mortes = 0;
        private int _nascimentos = 0;
        private int _entradas = 0;
        private int _saidas = 0;
        private string _produtorNome = string.Empty;
        private int _produtorId = 0;
        private bool _isEditMode = false;

        public Action? FecharJanela { get; set; }
        public Action? AtualizarDashboard { get; set; }

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public string ProdutorNome
        {
            get => _produtorNome;
            set
            {
                if (_produtorNome != value)
                {
                    _produtorNome = value;
                    OnPropertyChanged(nameof(ProdutorNome));
                }
            }
        }

        public int ProdutorId
        {
            get => _produtorId;
            set
            {
                if (_produtorId != value)
                {
                    _produtorId = value;
                    OnPropertyChanged(nameof(ProdutorId));
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => _isEditMode = value;
        }

        public string IdRebanho
        {
            get => _idRebanho;
            set
            {
                if (_idRebanho != value)
                {
                    _idRebanho = value;
                    OnPropertyChanged(nameof(IdRebanho));
                }
            }
        }

        public string NomeRebanho
        {
            get => _nomeRebanho;
            set
            {
                if (_nomeRebanho != value)
                {
                    _nomeRebanho = value;
                    OnPropertyChanged(nameof(NomeRebanho));
                }
            }
        }

        public int Mortes
        {
            get => _mortes;
            set
            {
                if (_mortes != value)
                {
                    _mortes = value;
                    OnPropertyChanged(nameof(Mortes));
                }
            }
        }

        public int Nascimentos
        {
            get => _nascimentos;
            set
            {
                if (_nascimentos != value)
                {
                    _nascimentos = value;
                    OnPropertyChanged(nameof(Nascimentos));
                }
            }
        }

        public int Entradas
        {
            get => _entradas;
            set
            {
                if (_entradas != value)
                {
                    _entradas = value;
                    OnPropertyChanged(nameof(Entradas));
                }
            }
        }

        public int Saidas
        {
            get => _saidas;
            set
            {
                if (_saidas != value)
                {
                    _saidas = value;
                    OnPropertyChanged(nameof(Saidas));
                }
            }
        }

        public ICommand SalvarCommand { get; }
        public ICommand LimparCommand { get; }

        public RebanhoViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
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
                var repo = scope.ServiceProvider.GetRequiredService<IRebanhoRepository>();

                if (_isEditMode)
                {
                    // Atualizar rebanho existente
                    var rebanhoExistente = await repo.GetByIdAsync(Id);
                    if (rebanhoExistente == null)
                    {
                        AlertService.Show("Registro não encontrado.", "Erro", AlertType.Error);
                        return;
                    }

                    rebanhoExistente.IdRebanho = IdRebanho;
                    rebanhoExistente.NomeRebanho = NomeRebanho;
                    rebanhoExistente.Mortes = Mortes;
                    rebanhoExistente.Nascimentos = Nascimentos;
                    rebanhoExistente.Entradas = Entradas;
                    rebanhoExistente.Saidas = Saidas;
                    rebanhoExistente.ProdutorId = ProdutorId;

                    await repo.UpdateAsync(rebanhoExistente);
                    AlertService.Show($"Propriedade '{NomeRebanho}' atualizada com sucesso!", "Sucesso", AlertType.Success);
                }
                else
                {
                    // Criar novo rebanho
                    var rebanho = new Rebanho
                    {
                        IdRebanho = IdRebanho,
                        NomeRebanho = NomeRebanho,
                        Mortes = Mortes,
                        Nascimentos = Nascimentos,
                        Entradas = Entradas,
                        Saidas = Saidas,
                        ProdutorId = ProdutorId
                    };

                    await repo.AddAsync(rebanho);
                    AlertService.Show($"Propriedade '{NomeRebanho}' cadastrada com sucesso para o produtor '{ProdutorNome}'!", "Sucesso", AlertType.Success);
                }
                
                // Atualizar o dashboard
                AtualizarDashboard?.Invoke();
                
                // Fechar a janela
                FecharJanela?.Invoke();
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao salvar propriedade: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private bool CanSalvar()
        {
            return !string.IsNullOrWhiteSpace(IdRebanho) && 
                   !string.IsNullOrWhiteSpace(NomeRebanho) &&
                   ProdutorId > 0;
        }

        private bool ValidarCampos()
        {
            if (ProdutorId <= 0)
            {
                AlertService.Show("Produtor invalido. Por favor, selecione um produtor.", "Validacao", AlertType.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(IdRebanho))
            {
                AlertService.Show("Por favor, preencha o ID do rebanho.", "Validacao", AlertType.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(NomeRebanho))
            {
                AlertService.Show("Por favor, preencha o nome do rebanho.", "Validacao", AlertType.Warning);
                return false;
            }

            if (Mortes < 0 || Nascimentos < 0 || Entradas < 0 || Saidas < 0)
            {
                AlertService.Show("Os valores de mortes, nascimentos, entradas e saidas nao podem ser negativos.", "Validacao", AlertType.Warning);
                return false;
            }

            return true;
        }

        private void Limpar()
        {
            IdRebanho = string.Empty;
            NomeRebanho = string.Empty;
            Mortes = 0;
            Nascimentos = 0;
            Entradas = 0;
            Saidas = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
