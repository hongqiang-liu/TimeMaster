using Microsoft.Win32;
using System.Diagnostics;

namespace TimeMaster;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppKeyName = "TimeMaster";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(AppKeyName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return;
                }

                key?.SetValue(AppKeyName, $"\"{executablePath}\"");
            }
            else
            {
                key?.DeleteValue(AppKeyName, false);
            }
        }
        catch
        {
            // Ignore registry write failures to avoid blocking app startup.
        }
    }
}
