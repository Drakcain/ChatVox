namespace ChatVox.Twitch;

/// <summary>
/// Audited, high-confidence handle fragments. Values are speech renderings, not
/// Twitch identities; anything absent from this table stays available to the
/// conservative normalizer or the user's local pronunciation override.
/// </summary>
internal static class UsernameLexicon
{
    private static readonly IReadOnlyDictionary<string, string> LeetLexemes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["bl00m"] = "bloom",
        ["cr1t1cal"] = "critical",
        ["cr1t"] = "crit",
        ["d0ll"] = "doll",
        ["d0ve"] = "dove",
        ["d4rkn3ss"] = "darkness",
        ["d4rkness"] = "darkness",
        ["d4rk"] = "dark",
        ["darkness"] = "darkness",
        ["d34th"] = "death",
        ["d3ad"] = "dead",
        ["f4e"] = "fae",
        ["g4m3r"] = "gamer",
        ["gh0st"] = "ghost",
        ["gh0ul"] = "ghoul",
        ["h0llow"] = "hollow",
        ["h3x"] = "hex",
        ["h4xx0r"] = "hacker",
        ["h4x0r"] = "hacker",
        ["h4x"] = "hack",
        ["k1tty"] = "kitty",
        ["k4t"] = "cat",
        ["l33t"] = "leet",
        ["l1l"] = "lil",
        ["m0mma"] = "momma",
        ["m0th"] = "moth",
        ["m0m"] = "mom",
        ["m3"] = "me",
        ["m4g1c"] = "magic",
        ["n00b"] = "noob",
        ["n0cturne"] = "nocturne",
        ["n0ob"] = "noob",
        ["n1ght"] = "night",
        ["n3on"] = "neon",
        ["ph34rm3"] = "fear me",
        ["ph34r"] = "fear",
        ["r0gu3"] = "rogue",
        ["r0tten"] = "rotten",
        ["r0t"] = "rot",
        ["r4re"] = "rare",
        ["sk8r"] = "skater",
        ["sp0re"] = "spore",
        ["squ1rr3l"] = "squirrel",
        ["v01d"] = "void",
        ["vvitch"] = "witch",
        ["w1sp"] = "wisp",
        ["w1tch"] = "witch"
    };

    public static string DecodeHighConfidenceLexemes(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return token;

        var normalized = token.ToLowerInvariant();
        var matches = LeetLexemes
            .OrderByDescending(pair => pair.Key.Length)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        var parts = new List<string>();
        var raw = new System.Text.StringBuilder();
        var index = 0;
        var changed = false;

        while (index < normalized.Length)
        {
            var match = matches.FirstOrDefault(pair => normalized.AsSpan(index).StartsWith(pair.Key, StringComparison.Ordinal));
            if (match.Key is null)
            {
                raw.Append(token[index]);
                index++;
                continue;
            }

            if (raw.Length > 0)
            {
                parts.Add(raw.ToString());
                raw.Clear();
            }

            parts.Add(match.Value);
            index += match.Key.Length;
            changed = true;
        }

        if (raw.Length > 0) parts.Add(raw.ToString());
        return changed ? string.Join(' ', parts) : token;
    }
}
