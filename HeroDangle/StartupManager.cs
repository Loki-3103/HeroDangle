using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace HeroDangle;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "HeroDangle";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
            return;

        if (enabled)
            key.SetValue(ValueName, $"\"{ExecutablePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }

    private static string ExecutablePath
    {
        get
        {
            string? process = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(process))
                return process;

            return Path.ChangeExtension(Process.GetCurrentProcess().MainModule?.FileName ?? "", ".exe");
        }
    }
}
