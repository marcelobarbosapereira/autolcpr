using System;
using System.ComponentModel;
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
    /// ViewModel para cadastro de produtores
    /// </summary>
    public class ProdutorViewModel : INotifyPropertyChanged
    {
        private int _id = 0;
        private string _nome = string.Empty;
        private bool _isEditMode = false;
        private readonly IServiceProvider? _serviceProvider;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => _isEditMode = value;
        }

        public string Nome
        {
            get => _nome;
            set
            {
                if (_nome != value)
                {
                    _nome = value;
                    OnPropertyChanged(nameof(Nome));
                }
            }
        }

        public ICommand SalvarCommand { get; }
        public ICommand LimparCommand { get; }

        public ProdutorViewModel()
        {
            _serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
            SalvarCommand = new RelayCommand(Salvar, CanSalvar);
            LimparCommand = new RelayCommand(Limpar);
        }

        private async void Salvar()
        {
            var nome = Nome.Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                AlertService.Show("Por favor, preencha o nome do produtor.", "Validacao", AlertType.Warning);
                return;
            }

            if (_serviceProvider == null)
            {
                AlertService.Show("Servicos nao inicializados.", "Erro", AlertType.Error);
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IProdutorRepository>();
                
                if (_isEditMode)
                {
                    // Modo de edição - atualizar produtor existente
                    var produtorExistente = await repo.GetByIdAsync(Id);
                    if (produtorExistente == null)
                    {
                        AlertService.Show("Produtor nao encontrado.", "Erro", AlertType.Error);
                        return;
                    }

                    // Verificar se o novo nome já existe em outro produtor
                    var existente = await repo.GetByNomeAsync(nome);
                    if (existente != null && existente.Id != Id && string.Equals(existente.Nome, nome, StringComparison.OrdinalIgnoreCase))
                    {
                        AlertService.Show("Ja existe outro produtor com esse nome.", "Validacao", AlertType.Warning);
                        return;
                    }

                    produtorExistente.Nome = nome;
                    await repo.UpdateAsync(produtorExistente);
                    AlertService.Show($"Produtor '{nome}' atualizado com sucesso!", "Sucesso", AlertType.Success);
                }
                else
                {
                    // Modo de criação - adicionar novo produtor
                    var existente = await repo.GetByNomeAsync(nome);
                    if (existente != null && string.Equals(existente.Nome, nome, StringComparison.OrdinalIgnoreCase))
                    {
                        AlertService.Show("Ja existe um produtor com esse nome.", "Validacao", AlertType.Warning);
                        return;
                    }

                    await repo.AddAsync(new Produtor
                    {
                        Nome = nome,
                        InscricaoEstadual = $"NAO-INFORMADA-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
                    });
                    AlertService.Show($"Produtor '{nome}' cadastrado com sucesso!", "Sucesso", AlertType.Success);
                }
                
                Limpar();
            }
            catch (Exception ex)
            {
                AlertService.Show($"Erro ao salvar produtor: {ex.Message}", "Erro", AlertType.Error);
            }
        }

        private bool CanSalvar()
        {
            return !string.IsNullOrWhiteSpace(Nome);
        }

        private void Limpar()
        {
            Nome = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

