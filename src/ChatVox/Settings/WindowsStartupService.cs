using Microsoft.Win32;
using System.IO;

namespace ChatVox.Settings;

public sealed record StartupState(bool Enabled, bool NeedsRepair, string? Command);

/// <summary>Owns only the per-user ChatVox Run value; never touches other startup entries.</summary>
public sealed class WindowsStartupService
{
    internal const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "ChatVox";

    public static string CurrentCommand(string? executablePath = null)
    {
        var path = executablePath ?? Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "ChatVox.exe");
        return $"\"{Path.GetFullPath(path)}\"";
    }

    public StartupState Read(string? executablePath = null)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunPath, false);
        var value = key?.GetValue(ValueName) as string;
        var expected = CurrentCommand(executablePath);
        return new(!string.IsNullOrWhiteSpace(value) && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase),
            !string.IsNullOrWhiteSpace(value) && !string.Equals(value, expected, StringComparison.OrdinalIgnoreCase), value);
    }

    public void SetEnabled(bool enabled, string? executablePath = null)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunPath, true) ?? throw new InvalidOperationException("Unable to open the per-user Windows startup registry key.");
        if (enabled) key.SetValue(ValueName, CurrentCommand(executablePath), RegistryValueKind.String);
        else key.DeleteValue(ValueName, false);
    }
}
