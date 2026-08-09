using System.IO;
using System.Text.Json;
using ChatVox.Updates;

namespace ChatVox.Settings;

public enum AppearanceMode { System, Dark, Light }

public sealed class AppSettings
{
    public static readonly string[] DefaultIgnoredUsers = ["StreamElements", "Nightbot", "SoundAlerts", "Sery_Bot", "PlayWithViewersBot"];
    public AppearanceMode Appearance { get; set; } = AppearanceMode.Dark;
    public string Voice { get; set; } = Speech.Voices.Default;
    public double Speed { get; set; } = 1.0;
    public double Volume { get; set; } = 1.0;
    public bool ReadUsernames { get; set; } = true;
    public int MaxPending { get; set; } = 6;
    public int MaxAgeSeconds { get; set; } = 30;
    public int SpeechGapMilliseconds { get; set; } = 500;
    public int MaxMessageLength { get; set; } = 200;
    public bool IgnoreCommands { get; set; } = true;
    public bool IgnoreUrls { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool StartMinimizedToTray { get; set; }
    public bool AutomaticallyCheckForUpdates { get; set; } = true;
    public UpdateChannel UpdateChannel { get; set; } = UpdateChannel.Preview;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
    public DateTimeOffset? LastSuccessfulUpdateCheckUtc { get; set; }
    public string? LatestKnownEligibleRelease { get; set; }
    public string? UpdateEtag { get; set; }
    public List<string> IgnoredUsers { get; set; } = [.. DefaultIgnoredUsers];
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowWasMaximized { get; set; }

    public void Normalize()
    {
        if (!Enum.IsDefined(Appearance)) Appearance = AppearanceMode.Dark;
        Voice = Speech.Voices.Resolve(Voice);
        Speed = Math.Clamp(Speed, 0.5, 2.0);
        Volume = Math.Clamp(Volume, 0.0, 1.0);
        MaxPending = Math.Clamp(MaxPending, 1, 30);
        MaxAgeSeconds = Math.Clamp(MaxAgeSeconds, 1, 120);
        SpeechGapMilliseconds = Math.Clamp(SpeechGapMilliseconds, 0, 5000);
        MaxMessageLength = Math.Clamp(MaxMessageLength, 1, 500);
        IgnoredUsers ??= [.. DefaultIgnoredUsers];
        IgnoredUsers = IgnoredUsers.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (LastUpdateCheckUtc is { } last && last > DateTimeOffset.UtcNow.AddDays(1)) LastUpdateCheckUtc = null;
        if (!Enum.IsDefined(UpdateChannel)) UpdateChannel = UpdateChannel.Preview;
        if (WindowWidth is <= 0) WindowWidth = null;
        if (WindowHeight is <= 0) WindowHeight = null;
        if (WindowLeft is not { } left || double.IsNaN(left) || double.IsInfinity(left)) WindowLeft = null;
        if (WindowTop is not { } top || double.IsNaN(top) || double.IsInfinity(top)) WindowTop = null;
    }
}

public sealed class AppSettingsStore(string? path = null)
{
    private readonly string path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatVox", "settings.json");

    public AppSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch { return new AppSettings(); }
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings));
        File.Move(temp, path, true);
    }
}
