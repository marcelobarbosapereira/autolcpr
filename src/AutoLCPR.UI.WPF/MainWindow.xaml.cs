using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using AutoLCPR.UI.WPF.ViewModels;

namespace AutoLCPR.UI.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return;
            }

            var workingArea = info.rcWork;
            var workingWidth = workingArea.Right - workingArea.Left;
            var workingHeight = workingArea.Bottom - workingArea.Top;

            WindowState = WindowState.Normal;
            Left = workingArea.Left;
            Top = workingArea.Top;
            Width = workingWidth / 2.0;
            Height = workingHeight;
        }

        private const int MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        private static extern System.IntPtr MonitorFromWindow(System.IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(System.IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
