using ChatVox.Filtering;
using ChatVox.Queue;
using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class Rc7PipelineTests
{
    [Fact]
    public void ReadUsernamesUsesActualChatterAndCanBeDisabled()
    {
        var chat = new ChatEvent("name-on", "ViewerDisplay", "hello everyone", DateTimeOffset.UtcNow);
        var withNames = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), withNames, new EventDeduplicator()).Accept(chat));
        Assert.Equal("ViewerDisplay said: hello everyone", withNames.Take(DateTimeOffset.UtcNow)!.Text);
        var withoutNames = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), withoutNames, new EventDeduplicator()) { ReadUsernames = false }.Accept(chat with { MessageId = "name-off" }));
        Assert.Equal("hello everyone", withoutNames.Take(DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void EmojiToggleControlsOnlyUnicodeEmojiFiltering()
    {
        var emojiOn = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), emojiOn, new EventDeduplicator()) { ReadUsernames = false, IgnoreEmoji = true }.Accept(new ChatEvent("emoji-on", "Viewer", "hello 😂 world", DateTimeOffset.UtcNow)));
        Assert.Equal("hello world", emojiOn.Take(DateTimeOffset.UtcNow)!.Text);
        var emojiOff = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), emojiOff, new EventDeduplicator()) { ReadUsernames = false, IgnoreEmoji = false }.Accept(new ChatEvent("emoji-off", "Viewer", "hello 😂 world", DateTimeOffset.UtcNow)));
        Assert.Equal("hello 😂 world", emojiOff.Take(DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void FilteredOrEmptyMessagesNeverQueueOnlyAUsername()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(["Nightbot"]), queue, new EventDeduplicator());
        Assert.False(pipeline.Accept(new ChatEvent("emoji-only", "Viewer", "😂", DateTimeOffset.UtcNow)));
        Assert.False(pipeline.Accept(new ChatEvent("ignored", "Nightbot", "hello", DateTimeOffset.UtcNow)));
        Assert.False(pipeline.Accept(new ChatEvent("command", "Viewer", "!hello", DateTimeOffset.UtcNow)));
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void StructuredTwitchEmotesRemainRemovedWhenEmojiFilteringIsOff()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"emote-mixed"},"payload":{"event":{"chatter_user_name":"ActualViewer","chatter_user_login":"actualviewer","message":{"text":"hello Kappa 😂","fragments":[{"text":"hello ","type":"text"},{"text":"Kappa","type":"emote","emote":{"id":"1"}},{"text":" 😂","type":"text"}]}}}}""";
        var chat = EventSubParser.Chat(json, DateTimeOffset.UtcNow)!;
        var queue = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()) { ReadUsernames = false, IgnoreEmoji = false }.Accept(chat));
        Assert.Equal("hello 😂", queue.Take(DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void ChatterLoginIsUsedWhenDisplayNameIsMissing()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"fallback"},"payload":{"event":{"chatter_user_name":"","chatter_user_login":"viewer_login","message":{"text":"hello"}}}}""";
        Assert.Equal("viewer_login", EventSubParser.Chat(json, DateTimeOffset.UtcNow)!.Chatter);
    }
}
