using System.Text.Json;
using ChatVox.Filtering;
namespace ChatVox.Twitch;
public static class EventSubParser
{
    public static string? WelcomeSession(string json){using var d=JsonDocument.Parse(json);return d.RootElement.GetProperty("payload").GetProperty("session").GetProperty("id").GetString();}
    public static string? ReconnectUrl(string json){using var d=JsonDocument.Parse(json);return d.RootElement.GetProperty("payload").GetProperty("session").GetProperty("reconnect_url").GetString();}
    public static ChatEvent? Chat(string json,DateTimeOffset now)
    {
        using var d=JsonDocument.Parse(json); var r=d.RootElement; var m=r.GetProperty("metadata");
        if(m.GetProperty("message_type").GetString()!="notification") return null;
        var e=r.GetProperty("payload").GetProperty("event"); var message=e.GetProperty("message");
        var text = StructuredText(message);
        var chatter = e.GetProperty("chatter_user_name").GetString();
        if (string.IsNullOrWhiteSpace(chatter) && e.TryGetProperty("chatter_user_login", out var login)) chatter = login.GetString();
        return new(m.GetProperty("message_id").GetString()??"",chatter?.Trim() is { Length: > 0 } name ? name : "Viewer",text,now);
    }
    private static string StructuredText(JsonElement message)
    {
        if (!message.TryGetProperty("fragments", out var fragments) || fragments.ValueKind != JsonValueKind.Array)
            return message.TryGetProperty("text", out var fallback) ? fallback.GetString() ?? string.Empty : string.Empty;
        var parts = new List<string>();
        foreach (var fragment in fragments.EnumerateArray())
        {
            // Twitch marks global, channel, subscription, and other emotes with type=emote.
            if (string.Equals(fragment.TryGetProperty("type", out var type) ? type.GetString() : null, "emote", StringComparison.OrdinalIgnoreCase)) continue;
            if (fragment.TryGetProperty("text", out var value)) parts.Add(value.GetString() ?? string.Empty);
        }
        return string.Concat(parts);
    }
}
