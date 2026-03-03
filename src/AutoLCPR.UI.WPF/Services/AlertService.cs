using System.Windows;
using AutoLCPR.UI.WPF.Views;

namespace AutoLCPR.UI.WPF.Services
{
    public enum AlertType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public static class AlertService
    {
        public static void Show(string message, string title, AlertType type)
        {
            var window = new AlertWindow(title, message, type);
            
            // Apenas definir Owner se a MainWindow existe e foi exibida
            var mainWindow = System.Windows.Application.Current?.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded)
            {
                window.Owner = mainWindow;
                window.ShowDialog();
            }
            else
            {
                // Se MainWindow não existe ou não foi carregada ainda, exibir sem Owner
                window.Show();
            }
        }
    }
}
