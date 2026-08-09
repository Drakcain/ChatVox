using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class UsernamePronunciationGoldTests
{
    [Fact]
    public void ApprovedGoldCorpusHasExactSpeechRenderings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "username-pronunciation-gold.tsv");
        Assert.True(File.Exists(path), $"Missing username pronunciation gold corpus: {path}");

        var rows = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(line => line.Split('\t'))
            .ToArray();

        Assert.NotEmpty(rows);
        foreach (var row in rows)
        {
            Assert.Equal(2, row.Length);
            Assert.Equal(row[1], UsernameSpeechNormalizer.Normalize(row[0]));
        }
    }
}
