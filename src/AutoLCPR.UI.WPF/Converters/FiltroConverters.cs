using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoLCPR.Domain.Entities;

namespace AutoLCPR.UI.WPF.Converters
{
    /// <summary>
    /// Converter para mudar a cor do texto baseado no filtro selecionado
    /// </summary>
    public class FiltroToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is string filtroAtual && parameter is string filtroParam)
            {
                return filtroAtual == filtroParam ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Color.FromRgb(95, 99, 104));
            }
            return new SolidColorBrush(Color.FromRgb(95, 99, 104));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para mudar a cor da linha indicadora baseado no filtro selecionado
    /// </summary>
    public class FiltroToLineColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is string filtro)
            {
                return filtro switch
                {
                    "Despesas" => new SolidColorBrush(Color.FromRgb(229, 62, 62)), // Vermelho
                    "Receitas" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Verde
                    "Rebanho" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Azul
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para controlar a largura da linha indicadora
    /// </summary>
    public class FiltroToWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is string filtro)
            {
                return filtro switch
                {
                    "Despesas" => 90,
                    "Receitas" => 95,
                    "Rebanho" => 85,
                    _ => 0
                };
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para mostrar/esconder as tabelas baseado no filtro selecionado
    /// </summary>
    public class FiltroToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is string filtroAtual && parameter is string filtroParam)
            {
                return filtroAtual == filtroParam ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para comparar se dois itens Rebanho são iguais (selecionado)
    /// </summary>
    public class RebanhoSelectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is Rebanho itemAtual && values[1] is Rebanho itemSelecionado)
            {
                return itemAtual.Id == itemSelecionado.Id;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para comparar se dois itens NotaFiscal são iguais (selecionado)
    /// </summary>
    public class NotaFiscalSelectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is NotaFiscal itemAtual && values[1] is NotaFiscal itemSelecionado)
            {
                return itemAtual.Id == itemSelecionado.Id;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para inverter um valor booleano
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter para converter booleano em Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
