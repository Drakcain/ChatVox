using System.Text;
using System.Text.RegularExpressions;

namespace ChatVox.Filtering;

public static class ChatTextSanitizer
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SpaceBeforePunctuation = new(@"\s+([,.;!?])", RegexOptions.Compiled);
    private static readonly Regex Keycap = new(@"[0-9#*]\uFE0F?\u20E3", RegexOptions.Compiled);
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var builder = new StringBuilder(text.Length);
        foreach (var rune in Keycap.Replace(text, string.Empty).EnumerateRunes()) if (!IsEmojiPresentation(rune)) builder.Append(rune.ToString());
        return SpaceBeforePunctuation.Replace(Whitespace.Replace(builder.ToString(), " ").Trim(), "$1");
    }
    private static bool IsEmojiPresentation(Rune rune)
    {
        var value = rune.Value;
        return value is 0x200D or 0x20E3 or 0xFE0E or 0xFE0F || (value >= 0x1F000 && value <= 0x1FAFF) || (value >= 0x2600 && value <= 0x27BF);
    }
}
