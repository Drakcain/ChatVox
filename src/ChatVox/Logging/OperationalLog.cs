using System.Text;
using System.IO;

namespace ChatVox.Logging;

public sealed class OperationalLog
{
    public const int MaxFiles = 5;
    public const long MaxBytesPerFile = 2 * 1024 * 1024;
    private readonly object sync = new();
    private readonly string directory;

    public OperationalLog(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatVox", "logs");
        Directory.CreateDirectory(this.directory);
    }

    public string DirectoryPath => directory;

    public void Write(string area, string message)
    {
        lock (sync)
        {
            if (area.Equals("CHAT", StringComparison.OrdinalIgnoreCase)) message = "chat transcript suppressed";
            if (message.Contains("access_token", StringComparison.OrdinalIgnoreCase) || message.Contains("refresh_token", StringComparison.OrdinalIgnoreCase) || message.Contains("device_code", StringComparison.OrdinalIgnoreCase) || message.Contains("authorization:", StringComparison.OrdinalIgnoreCase)) message = "sensitive log content suppressed";
            Directory.CreateDirectory(directory);
            var current = Path.Combine(directory, "chatvox.log");
            if (File.Exists(current) && new FileInfo(current).Length >= MaxBytesPerFile) Rotate(current);
            File.AppendAllText(current, $"{DateTimeOffset.UtcNow:O} [{area}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
    }

    private void Rotate(string current)
    {
        var oldest = Path.Combine(directory, $"chatvox.{MaxFiles - 1}.log");
        if (File.Exists(oldest)) File.Delete(oldest);
        for (var i = MaxFiles - 2; i >= 1; i--)
        {
            var source = Path.Combine(directory, $"chatvox.{i}.log");
            if (File.Exists(source)) File.Move(source, Path.Combine(directory, $"chatvox.{i + 1}.log"), true);
        }
        File.Move(current, Path.Combine(directory, "chatvox.1.log"), true);
    }
}
