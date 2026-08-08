using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatVox.Updates;

public enum UpdateChannel { Stable, Preview }

public sealed record UpdateCheckResult(bool IsConfigured, bool IsUpdateAvailable, string SafeMessage, string? AvailableVersion = null, Uri? InstallerUri = null, Uri? Sha256Uri = null, long InstallerBytes = 0);
public sealed record VerifiedUpdatePackage(string InstallerPath, string Version);

/// <summary>Anonymous, read-only GitHub Releases discovery and verified installer download.</summary>
public sealed class UpdateService
{
    public static readonly Uri ReleaseFeed = new("https://api.github.com/repos/Drakcain/ChatVox/releases");
    private readonly HttpClient http;

    public UpdateService(HttpClient? httpClient = null)
    {
        http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        if (!http.DefaultRequestHeaders.UserAgent.Any()) http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ChatVox", "1.0"));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public Task<UpdateCheckResult> CheckAsync(string currentVersion, CancellationToken ct) => CheckAsync(currentVersion, UpdateChannel.Preview, ct);

    public async Task<UpdateCheckResult> CheckAsync(string currentVersion, UpdateChannel channel, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(ReleaseFeed, ct);
            if (!response.IsSuccessStatusCode) return new(true, false, "Unable to check for updates right now.");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var latest = FindLatest(document.RootElement, currentVersion, channel);
            return latest is null ? new(true, false, "Up to date.") : latest;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return new(true, false, "Unable to check for updates right now."); }
    }

    public async Task<VerifiedUpdatePackage> DownloadAndVerifyAsync(UpdateCheckResult update, IProgress<int>? progress, CancellationToken ct)
    {
        if (!update.IsUpdateAvailable || update.InstallerUri is null || update.Sha256Uri is null || string.IsNullOrWhiteSpace(update.AvailableVersion)) throw new InvalidOperationException("No verified update is available.");
        if (update.InstallerUri.Scheme != Uri.UriSchemeHttps || update.Sha256Uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Update assets must use HTTPS.");
        var name = $"ChatVox-{update.AvailableVersion}-Setup.exe";
        if (!string.Equals(Path.GetFileName(update.InstallerUri.AbsolutePath), name, StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected installer asset.");
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatVox", "updates", "downloads");
        Directory.CreateDirectory(root);
        foreach (var partial in Directory.EnumerateFiles(root, "*.partial")) File.Delete(partial);
        var partialPath = Path.Combine(root, name + ".partial");
        var finalPath = Path.Combine(root, name);
        try
        {
            using var installerResponse = await http.GetAsync(update.InstallerUri, HttpCompletionOption.ResponseHeadersRead, ct);
            installerResponse.EnsureSuccessStatusCode();
            var length = installerResponse.Content.Headers.ContentLength ?? update.InstallerBytes;
            if (length <= 0) throw new InvalidOperationException("Installer size is invalid.");
            await using (var source = await installerResponse.Content.ReadAsStreamAsync(ct))
            await using (var target = File.Create(partialPath))
            {
                var buffer = new byte[81920]; long total = 0; int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0) { await target.WriteAsync(buffer.AsMemory(0, read), ct); total += read; progress?.Report((int)Math.Min(100, total * 100 / length)); }
                if (total != length) throw new InvalidOperationException("Installer download was incomplete.");
            }
            var shaText = await http.GetStringAsync(update.Sha256Uri, ct);
            var match = Regex.Match(shaText.Trim(), "^(?<hash>[A-Fa-f0-9]{64})\\s{2}(?<name>[^\\r\\n]+)$");
            if (!match.Success || !string.Equals(match.Groups["name"].Value, name, StringComparison.Ordinal)) throw new InvalidOperationException("Update checksum file is invalid.");
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(partialPath), ct));
            if (!string.Equals(actual, match.Groups["hash"].Value, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Update checksum did not match.");
            File.Move(partialPath, finalPath, true);
            return new(finalPath, update.AvailableVersion);
        }
        catch { if (File.Exists(partialPath)) File.Delete(partialPath); throw; }
    }

    private static UpdateCheckResult? FindLatest(JsonElement releases, string currentVersion, UpdateChannel channel)
    {
        UpdateCheckResult? best = null;
        foreach (var release in releases.EnumerateArray())
        {
            if (release.GetPropertyOrDefault("draft") || (channel == UpdateChannel.Stable && release.GetPropertyOrDefault("prerelease"))) continue;
            var tag = release.GetStringOrDefault("tag_name");
            if (tag is null || !TryParseVersion(tag.TrimStart('v'), out _)) continue;
            if (CompareVersions(tag.TrimStart('v'), currentVersion) <= 0) continue;
            var installerName = $"ChatVox-{tag.TrimStart('v')}-Setup.exe";
            var shaName = installerName + ".sha256";
            Uri? installer = null, sha = null; long bytes = 0;
            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetStringOrDefault("name"); var url = asset.GetStringOrDefault("browser_download_url");
                if (name == installerName && Uri.TryCreate(url, UriKind.Absolute, out var installerUri)) { installer = installerUri; bytes = asset.GetPropertyOrDefault("size", 0L); }
                if (name == shaName && Uri.TryCreate(url, UriKind.Absolute, out var shaUri)) sha = shaUri;
            }
            if (installer is null || sha is null || bytes <= 0) continue;
            var candidate = new UpdateCheckResult(true, true, $"Update available: ChatVox {tag.TrimStart('v')}.", tag.TrimStart('v'), installer, sha, bytes);
            if (best is null || CompareVersions(candidate.AvailableVersion!, best.AvailableVersion!) > 0) best = candidate;
        }
        return best;
    }

    public static int CompareVersions(string left, string right)
    {
        if (!TryParseVersion(left, out var a) || !TryParseVersion(right, out var b)) throw new FormatException("Invalid ChatVox version.");
        var core = a.Core.CompareTo(b.Core); if (core != 0) return core;
        if (a.Pre is null) return b.Pre is null ? 0 : 1;
        if (b.Pre is null) return -1;
        return ComparePrerelease(a.Pre, b.Pre);
    }

    private static int ComparePrerelease(string a, string b)
    {
        var ap = a.Split('.'); var bp = b.Split('.');
        for (var i = 0; i < Math.Max(ap.Length, bp.Length); i++)
        {
            if (i == ap.Length) return -1; if (i == bp.Length) return 1;
            var an = int.TryParse(ap[i], out var ai); var bn = int.TryParse(bp[i], out var bi);
            var comparison = an && bn ? ai.CompareTo(bi) : an ? -1 : bn ? 1 : string.Compare(ap[i], bp[i], StringComparison.OrdinalIgnoreCase);
            if (comparison != 0) return comparison;
        }
        return 0;
    }
    private sealed record ParsedVersion(Version Core, string? Pre);
    private static bool TryParseVersion(string value, out ParsedVersion result)
    {
        var match = Regex.Match(value, "^(?<core>\\d+\\.\\d+\\.\\d+)(?:-(?<pre>[0-9A-Za-z.-]+))?$");
        if (!match.Success) { result = null!; return false; }
        result = new(Version.Parse(match.Groups["core"].Value), match.Groups["pre"].Success ? match.Groups["pre"].Value : null); return true;
    }
}

internal static class JsonElementExtensions
{
    public static string? GetStringOrDefault(this JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    public static bool GetPropertyOrDefault(this JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
    public static long GetPropertyOrDefault(this JsonElement element, string property, long fallback) => element.TryGetProperty(property, out var value) && value.TryGetInt64(out var number) ? number : fallback;
}
