using System.Text.Json;
using ChatVox.Filtering;
using ChatVox.Queue;
using ChatVox.Settings;
using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class Rc12OwnMessageTests
{
    [Fact]
    public void OwnMessageByStableUserIdIsSkippedWhenEnabled()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()) { ConnectedUserId = "connected-42" };
        Assert.False(pipeline.Accept(new ChatEvent("own-id", "My Display", "hello", DateTimeOffset.UtcNow, "connected-42", "my_login")));
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void OwnMessageLoginFallbackIsCaseInsensitive()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()) { ConnectedLogin = "My_Login" };
        Assert.False(pipeline.Accept(new ChatEvent("own-login", "My Display", "hello", DateTimeOffset.UtcNow, null, "my_login")));
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void OwnMessageIsAllowedWhenSettingIsOff()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()) { ConnectedUserId = "connected-42", IgnoreOwnMessages = false, ReadUsernames = false };
        Assert.True(pipeline.Accept(new ChatEvent("own-off", "My Display", "hello", DateTimeOffset.UtcNow, "connected-42")));
        Assert.Equal("hello", queue.Take(DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void UnrelatedViewerStillSpeaksAndExistingFiltersRemainUnchanged()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(["Nightbot"]), queue, new EventDeduplicator()) { ConnectedUserId = "connected-42", ReadUsernames = false };
        Assert.True(pipeline.Accept(new ChatEvent("viewer", "Viewer", "hello", DateTimeOffset.UtcNow, "viewer-7", "viewer")));
        Assert.False(pipeline.Accept(new ChatEvent("ignored", "Nightbot", "hello", DateTimeOffset.UtcNow, "bot-1", "nightbot")));
        Assert.Equal("hello", queue.Take(DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void ParserCarriesStableIdentityAndNormalUsernameSpeechRemainsUnchanged()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"identity"},"payload":{"event":{"chatter_user_id":"viewer-7","chatter_user_name":"number1redstar","chatter_user_login":"number1redstar","message":{"text":"hello"}}}}""";
        var chat = EventSubParser.Chat(json, DateTimeOffset.UtcNow)!;
        Assert.Equal("viewer-7", chat.ChatterUserId);
        Assert.Equal("number1redstar", chat.ChatterLogin);
        Assert.Equal("number 1 red star", UsernameSpeechNormalizer.Normalize(chat.Chatter));
    }

    [Fact]
    public void FreshDefaultsUseEightAndExistingSavedQueueValueIsPreserved()
    {
        var fresh = new AppSettings();
        fresh.Normalize();
        Assert.Equal(8, fresh.MaxPending);
        Assert.Equal(8, new FreshQueue().Max);
        Assert.Equal(30, fresh.MaxAgeSeconds);
        var existing = JsonSerializer.Deserialize<AppSettings>("""{"MaxPending":6,"MaxAgeSeconds":30}""")!;
        existing.Normalize();
        Assert.Equal(6, existing.MaxPending);
        Assert.Equal(30, existing.MaxAgeSeconds);
        Assert.True(existing.IgnoreOwnMessages);
    }

    [Fact]
    public void DedupStillPreventsDuplicateSpeechForNormalEventSubMessages()
    {
        var queue = new FreshQueue();
        var pipeline = new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()) { ReadUsernames = false };
        var chat = new ChatEvent("same-event", "Viewer", "hello", DateTimeOffset.UtcNow, "viewer-7", "viewer");
        Assert.True(pipeline.Accept(chat));
        Assert.False(pipeline.Accept(chat));
        Assert.Equal(1, queue.Count);
    }
}
