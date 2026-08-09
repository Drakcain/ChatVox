using System.Text.RegularExpressions;
using DotNetG2P.English;

namespace ChatVox.Twitch;

/// <summary>
/// Produces a conservative, speech-only rendering of a Twitch handle. The raw
/// Twitch identity remains unchanged for filtering, authorization, and storage.
/// </summary>
public static class UsernameSpeechNormalizer
{
    private static readonly Lazy<EnglishG2PEngine> Dictionary = new(() => new EnglishG2PEngine());
    private static readonly HashSet<string> DecorativeSuffixes = new(StringComparer.OrdinalIgnoreCase) { "ttv", "tv", "irl", "live" };
    // A very small set of ordinary streaming/slang words absent from the bundled
    // CMU dictionary. These are words, not channel-specific aliases.
    private static readonly HashSet<string> SupplementalWords = new(StringComparer.OrdinalIgnoreCase) { "baddie", "hax", "haxor", "mom", "moth", "noob", "pxl", "snacc", "sqrl", "void", "vvitch", "wulf" };
    private static readonly IReadOnlyDictionary<string, string> CommonAbbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["pxl"] = "pixel",
        ["haxor"] = "hacker",
        ["snacc"] = "snack",
        ["sqrl"] = "squirrel",
        ["vvitch"] = "witch",
        ["wulf"] = "wolf"
    };
    private static readonly IReadOnlyDictionary<char, char> Leetspeak = new Dictionary<char, char>
    {
        ['0'] = 'o', ['1'] = 'i', ['3'] = 'e', ['4'] = 'a', ['5'] = 's', ['7'] = 't'
    };

    public static string? Normalize(string? value)
    {
        var identity = ChatIdentity.Normalize(value);
        if (identity is null) return null;

        identity = TrimAttachedDecorativeSuffix(identity);
        identity = DecodeLeetspeakWhenItCreatesWords(identity);
        identity = UsernameLexicon.DecodeHighConfidenceLexemes(identity);
        identity = TrimDecorativeXWrapper(identity);
        identity = TrimDecorativeXWrapper(identity);
        identity = NormalizeDecorativeCasing(identity);

        // Do not split digit boundaries until after testing whether the digits are
        // leetspeak. For example, V01dM0th can become VoidMoth; number1redstar
        // remains a real number because its decoded alternative is not useful.
        var structural = Regex.Replace(identity, "(?<=[a-z])(?=[A-Z])|[_\\-.]+", " ");
        var structuralTokens = Regex.Matches(structural, "[A-Za-z0-9]+")
            .Select(match => UsernameLexicon.DecodeHighConfidenceLexemes(match.Value))
            .SelectMany(value => Regex.Matches(value, "[A-Za-z0-9]+").Cast<Match>())
            .Select(match => DecodeLeetspeakWhenItCreatesWords(match.Value))
            .ToList();

        while (structuralTokens.Count >= 3 && IsDecorativeXToken(structuralTokens[0]) && IsDecorativeXToken(structuralTokens[^1]))
        {
            structuralTokens.RemoveAt(structuralTokens.Count - 1);
            structuralTokens.RemoveAt(0);
        }

        var tokens = structuralTokens
            .SelectMany(SplitWordAndNumberBoundaries)
            .Select(match => match.Value.ToLowerInvariant())
            .ToList();

        while (tokens.Count > 1 && DecorativeSuffixes.Contains(tokens[^1])) tokens.RemoveAt(tokens.Count - 1);

        for (var index = 0; index < tokens.Count; index++)
        {
            tokens[index] = DecodeLeetspeakWhenItCreatesWords(tokens[index]);
            if (tokens[index].Length > 3 && tokens[index].StartsWith("blk", StringComparison.Ordinal) && !Dictionary.Value.ContainsWord(tokens[index]))
            {
                tokens[index] = "black" + tokens[index][3..];
            }

            tokens[index] = ExpandCommonAbbreviations(SplitCompound(tokens[index]));
        }

        return string.Join(' ', tokens);
    }

    private static string TrimAttachedDecorativeSuffix(string identity)
    {
        var trimmed = identity.TrimEnd('_', '-', '.');
        foreach (var suffix in new[] { "ttv", "xxx", "tv", "xx" })
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && trimmed.Length > suffix.Length + 3)
            {
                var candidate = trimmed[..^suffix.Length].TrimEnd('_', '-', '.');
                var isUnambiguousSuffix = suffix is "ttv" or "tv" or "xxx";
                if (isUnambiguousSuffix ? candidate.Any(char.IsLetter) : HasUsefulWordSplit(candidate)) trimmed = candidate;
            }
        }

        return trimmed;
    }

    private static string TrimDecorativeXWrapper(string identity)
    {
        identity = identity.Trim('_', '-', '.');
        var separatedWrapper = Regex.Match(identity, @"^x{1,2}[_\-.]+(?<body>.+?)[_\-.]+x{1,2}$", RegexOptions.IgnoreCase);
        if (separatedWrapper.Success && separatedWrapper.Groups["body"].Value.Any(char.IsLetter)) return separatedWrapper.Groups["body"].Value;

        foreach (var wrapperLength in new[] { 2, 1 })
        {
            if (identity.Length < (wrapperLength * 2) + 3 || !identity.StartsWith("x", StringComparison.OrdinalIgnoreCase) || !identity.EndsWith("x", StringComparison.OrdinalIgnoreCase)) continue;
            var prefix = identity[..wrapperLength];
            var suffix = identity[^wrapperLength..];
            if (!prefix.All(character => character is 'x' or 'X') || !suffix.All(character => character is 'x' or 'X')) continue;
            var candidate = identity[wrapperLength..^wrapperLength];
            if (IsKnownWord(candidate) || HasUsefulWordSplit(candidate)) return candidate;
        }

        return identity;
    }

    private static string NormalizeDecorativeCasing(string identity)
    {
        var transitions = Regex.Matches(identity, "(?<=[a-z])(?=[A-Z])").Count;
        return transitions >= 3 ? identity.ToLowerInvariant() : identity;
    }

    private static string DecodeLeetspeakWhenItCreatesWords(string token)
    {
        if (token.Length < 3 || !token.Any(char.IsDigit)) return token;
        var characters = token.ToCharArray();
        var changed = false;
        for (var index = 0; index < characters.Length; index++)
        {
            if (Leetspeak.TryGetValue(characters[index], out var replacement))
            {
                characters[index] = token.All(character => !char.IsLetter(character) || char.IsUpper(character))
                    ? char.ToUpperInvariant(replacement)
                    : replacement;
                changed = true;
            }
        }

        var candidate = new string(characters);
        return changed && (IsKnownWord(candidate) || HasUsefulWordSplit(candidate)) ? candidate : token;
    }

    private static IEnumerable<Match> SplitWordAndNumberBoundaries(string token)
    {
        var spaced = Regex.Replace(token, "(?<=[a-z])(?=[A-Z])|(?<=[A-Za-z])(?=\\d)|(?<=\\d)(?=[A-Za-z])", " ");
        return Regex.Matches(spaced, "[A-Za-z]+|\\d+").Cast<Match>();
    }

    private static bool IsDecorativeXToken(string token) => token.Equals("x", StringComparison.OrdinalIgnoreCase) || token.Equals("xx", StringComparison.OrdinalIgnoreCase);

    private static string ExpandCommonAbbreviations(string text) => string.Join(' ', text
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => CommonAbbreviations.TryGetValue(word, out var expanded) ? expanded : word));

    private static bool HasUsefulWordSplit(string token)
    {
        var normalized = token.ToLowerInvariant();
        return SplitCompound(normalized) != normalized;
    }

    private static string SplitCompound(string token)
    {
        if (token.Length < 7 || !token.All(char.IsLetter) || IsKnownWord(token)) return token;

        var best = new Candidate?[token.Length + 1];
        best[0] = new Candidate(string.Empty, 0, 0);

        for (var start = 0; start < token.Length; start++)
        {
            var previous = best[start];
            if (previous is null) continue;

            for (var end = start + 3; end <= token.Length; end++)
            {
                var word = token[start..end];
                if (!IsKnownWord(word)) continue;

                var score = previous.Score + (word.Length * word.Length);
                var candidate = new Candidate(previous.Text.Length == 0 ? word : $"{previous.Text} {word}", previous.WordCount + 1, score);
                if (best[end] is null || candidate.Score > best[end]!.Score) best[end] = candidate;
            }
        }

        var result = best[^1];
        return result is { WordCount: >= 2 } ? result.Text : token;
    }

    private static bool IsKnownWord(string word)
    {
        var normalized = word.ToLowerInvariant();
        return SupplementalWords.Contains(normalized) || Dictionary.Value.ContainsWord(normalized);
    }

    private sealed record Candidate(string Text, int WordCount, int Score);
}
