using System.Text.RegularExpressions;
using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class UsernamePronunciationCorpusTests
{
    [Fact]
    public void TwitchStyleCorpusIsAlwaysSafeForTheSpeechPipeline()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Data", "username-pronunciation-corpus.txt");
        Assert.True(File.Exists(corpusPath), $"Missing username pronunciation corpus: {corpusPath}");

        var handles = File.ReadLines(corpusPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseHandle)
            .ToArray();

        Assert.Equal(600, handles.Length);

        foreach (var handle in handles)
        {
            var spoken = UsernameSpeechNormalizer.Normalize(handle);
            Assert.False(string.IsNullOrWhiteSpace(spoken));
            Assert.Matches("^[a-z0-9 ]+$", spoken!);
            Assert.Matches("[a-z0-9]", spoken!);
            Assert.Equal(spoken, UsernameSpeechNormalizer.Normalize(spoken));
        }
    }

    private static string ParseHandle(string line)
    {
        var match = Regex.Match(line, @"^\d{3}\s+(?<handle>\S+)$");
        Assert.True(match.Success, $"Invalid corpus line: {line}");
        return match.Groups["handle"].Value;
    }
}
