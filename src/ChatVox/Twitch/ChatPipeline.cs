using ChatVox.Filtering; using ChatVox.Queue;
namespace ChatVox.Twitch;
public sealed class ChatPipeline(ChatFilter filter,FreshQueue queue,EventDeduplicator dedup)
{
    public bool ReadUsernames { get; set; } = true;
    public bool IgnoreEmoji { get; set; } = true;
    public int MaxMessageLength { get; set; } = 200;

    public bool Accept(ChatEvent chat)
    {
        if (!dedup.First(chat.MessageId, chat.Received)) return false;
        var text = ChatTextSanitizer.Normalize(chat.Text, IgnoreEmoji);
        if (string.IsNullOrWhiteSpace(text) || !filter.Accept(chat.Chatter, text)) return false;
        var speech = ReadUsernames && !string.IsNullOrWhiteSpace(chat.Chatter) ? $"{chat.Chatter.Trim()} said: {text}" : text;
        if (speech.Length > MaxMessageLength) return false;
        queue.Add(speech, chat.Received);
        return true;
    }
}
