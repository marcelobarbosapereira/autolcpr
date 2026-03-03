using System.IO;

namespace AutoLCPR.UI.WPF.Services;

public static class SimpleLogger
{
    private static readonly object Sync = new();

    public static void Log(string message)
    {
        Write("INFO", message, null);
    }

    public static void LogError(string message, Exception? ex)
    {
        Write("ERROR", message, ex);
    }

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoLCPR");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "app.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

            lock (Sync)
            {
                File.AppendAllText(logPath, line);
                if (ex != null)
                {
                    File.AppendAllText(logPath, ex + Environment.NewLine);
                }
            }
        }
        catch
        {
        }
    }
}
