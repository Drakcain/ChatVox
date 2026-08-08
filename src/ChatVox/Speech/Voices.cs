using System.Globalization;
using System.IO;

namespace ChatVox.Speech;

public sealed record VoiceOption(string Id, string FriendlyName, string Category, int CategoryOrder, string AssetPath)
{
    public string DisplayName => $"{FriendlyName} ({Category})";
    public int WithinCategoryOrder => string.Equals(Id, Voices.Default, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
}

public static class Voices
{
    public const string Default = "af_heart";
    public const int ExpectedEnglishVoiceCount = 28;

    private static readonly IReadOnlyDictionary<string, (string Category, int Order)> Categories = new Dictionary<string, (string, int)>
    {
        ["af"] = ("American Female", 10), ["am"] = ("American Male", 20),
        ["bf"] = ("British Female", 30), ["bm"] = ("British Male", 40)
    };

    private static readonly Lazy<IReadOnlyList<VoiceOption>> catalog = new(BuildCatalog);

    public static IReadOnlyList<VoiceOption> All => catalog.Value;

    public static string Resolve(object? selection) => selection switch
    {
        VoiceOption option when All.Any(x => string.Equals(x.Id, option.Id, StringComparison.OrdinalIgnoreCase)) => option.Id,
        string value when All.Any(x => string.Equals(x.DisplayName, value, StringComparison.OrdinalIgnoreCase)) => All.First(x => string.Equals(x.DisplayName, value, StringComparison.OrdinalIgnoreCase)).Id,
        string value when All.Any(x => string.Equals(x.Id, value, StringComparison.OrdinalIgnoreCase)) => All.First(x => string.Equals(x.Id, value, StringComparison.OrdinalIgnoreCase)).Id,
        _ => Default
    };

    public static VoiceOption OptionFor(string voiceId) => All.FirstOrDefault(x => string.Equals(x.Id, voiceId, StringComparison.OrdinalIgnoreCase)) ?? All.First(x => x.Id == Default);
    public static string LabelFor(string voiceId) => OptionFor(voiceId).DisplayName;
    public static bool IsBritish(string voiceId) => voiceId.StartsWith("bf_", StringComparison.OrdinalIgnoreCase) || voiceId.StartsWith("bm_", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<VoiceOption> BuildCatalog()
    {
        var voicesPath = Path.Combine(AppContext.BaseDirectory, "voices");
        var options = Directory.Exists(voicesPath)
            ? Directory.EnumerateFiles(voicesPath, "*.npy", SearchOption.TopDirectoryOnly)
                .Select(path => CreateOption(Path.GetFileNameWithoutExtension(path)!, path))
                .Where(option => option is not null)
                .Cast<VoiceOption>()
            : Fallback();

        var result = options
            .DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.CategoryOrder)
            .ThenBy(x => x.WithinCategoryOrder)
            .ThenBy(x => x.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result.Count == 0 ? Fallback().ToList() : result;
    }

    private static VoiceOption? CreateOption(string id, string assetPath)
    {
        var pieces = id.Split('_', 2);
        if (pieces.Length != 2 || !Categories.TryGetValue(pieces[0], out var metadata)) return null;
        var name = string.Equals(id, Default, StringComparison.OrdinalIgnoreCase)
            ? "Default"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pieces[1].Replace('-', ' '));
        return new VoiceOption(id, name, metadata.Category, metadata.Order, assetPath);
    }

    private static IEnumerable<VoiceOption> Fallback()
    {
        yield return new VoiceOption(Default, "Default", "American Female", 10, string.Empty);
        yield return new VoiceOption("af_bella", "Bella", "American Female", 10, string.Empty);
        yield return new VoiceOption("af_nicole", "Nicole", "American Female", 10, string.Empty);
        yield return new VoiceOption("af_sarah", "Sarah", "American Female", 10, string.Empty);
        yield return new VoiceOption("af_sky", "Sky", "American Female", 10, string.Empty);
    }
}
