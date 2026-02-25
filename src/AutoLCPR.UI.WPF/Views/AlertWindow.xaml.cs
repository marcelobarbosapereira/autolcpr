using System.Windows;
using System.Windows.Input;
using AutoLCPR.UI.WPF.Services;

namespace AutoLCPR.UI.WPF.Views
{
    public partial class AlertWindow : Window
    {
        public AlertWindow(string title, string message, AlertType type)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;

            ApplyTypeStyle(type);
        }

        private void ApplyTypeStyle(AlertType type)
        {
            switch (type)
            {
                case AlertType.Success:
                    IconText.Text = "OK";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 151, 71));
                    break;
                case AlertType.Warning:
                    IconText.Text = "!";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 136, 46));
                    break;
                case AlertType.Info:
                    IconText.Text = "i";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 113, 231));
                    break;
                case AlertType.Error:
                default:
                    IconText.Text = "X";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(213, 67, 62));
                    break;
            }
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
