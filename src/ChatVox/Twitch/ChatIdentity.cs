using System.Globalization;

namespace ChatVox.Twitch;

public static class ChatIdentity
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var visible = string.Concat(value.Where(character =>
        {
            var category = char.GetUnicodeCategory(character);
            return category is not UnicodeCategory.Control and not UnicodeCategory.Format;
        })).Trim();
        return visible.Any(char.IsLetterOrDigit) ? visible : null;
    }

}
