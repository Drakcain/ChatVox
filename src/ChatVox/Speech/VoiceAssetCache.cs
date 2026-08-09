using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ChatVox.Speech;

/// <summary>Keeps Kokoro's mutable voice runtime state outside Program Files.</summary>
public static class VoiceAssetCache
{
    public static VoiceCacheResult Ensure(string sourceDirectory, string cacheDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException("Bundled Kokoro voices are missing.");
        Directory.CreateDirectory(cacheDirectory);
        var assets = Directory.EnumerateFiles(sourceDirectory, "*.npy", SearchOption.TopDirectoryOnly).ToArray();
        var expectedNames = assets.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var copied = 0;
        var obsoleteAssets = Directory.EnumerateFiles(cacheDirectory, "*.npy", SearchOption.TopDirectoryOnly).Where(path => !expectedNames.Contains(Path.GetFileName(path))).ToArray();
        foreach (var obsolete in obsoleteAssets)
            File.Delete(obsolete);
        foreach (var asset in assets)
        {
            var target = Path.Combine(cacheDirectory, Path.GetFileName(asset));
            if (FilesMatch(asset, target)) continue;
            var temporary = target + ".tmp";
            File.Copy(asset, temporary, true);
            if (!FilesMatch(asset, temporary)) throw new IOException("Voice cache copy verification failed.");
            File.Move(temporary, target, true);
            copied++;
        }
        return new VoiceCacheResult(copied, obsoleteAssets.Length);
    }

    private static bool FilesMatch(string source, string candidate)
    {
        if (!File.Exists(candidate) || new FileInfo(source).Length != new FileInfo(candidate).Length) return false;
        using var sourceStream = File.OpenRead(source);
        using var candidateStream = File.OpenRead(candidate);
        return sourceStream.Length == candidateStream.Length && SHA256.HashData(sourceStream).SequenceEqual(SHA256.HashData(candidateStream));
    }
}

public readonly record struct VoiceCacheResult(int RefreshedAssets, int RemovedObsoleteAssets);
