using ChatVox.Filtering;
using ChatVox.Queue;
using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class Rc7PipelineTests
{
    [Theory]
    [InlineData("blkdragon76_ttv", "black dragon 76")]
    [InlineData("austingamingxxx", "austin gaming")]
    [InlineData("kinggh0stttv", "king ghost")]
    [InlineData("xgothbaddiex", "goth baddie")]
    [InlineData("xgothbaddiex_ttv", "goth baddie")]
    [InlineData("charon_xxx", "charon")]
    [InlineData("chelubtv", "chelub")]
    [InlineData("Squ1rr3lM0m", "squirrel mom")]
    [InlineData("gh0stpetal_ttv", "ghost petal")]
    [InlineData("Xx__V01dM0th__xX", "void moth")]
    [InlineData("L33tHaxor", "leet hacker")]
    [InlineData("CaptainN00b", "captain noob")]
    [InlineData("SQRLM0M", "squirrel mom")]
    [InlineData("Xx__Gh0stBeyondVoid__xX", "ghost beyond void")]
    [InlineData("number1redstar", "number 1 red star")]
    [InlineData("Bl00m", "bloom")]
    [InlineData("G4m3rM0m", "gamer mom")]
    [InlineData("Pix3lPirate77", "pixel pirate 77")]
    [InlineData("xxVoidWalkerxx", "void walker")]
    [InlineData("xSqUiRrElx", "squirrel")]
    [InlineData("Xx__xHushx__Xx", "hush")]
    [InlineData("Xx_Gh0stPxl_Xx", "ghost pixel")]
    [InlineData("Xx_CryptSnacc_xX", "crypt snack")]
    [InlineData("Xx__W1spWulf__Xx", "wisp wolf")]
    [InlineData("x__VV1TCH__x", "witch")]
    [InlineData("H4xM0de", "hax mode")]
    [InlineData("SilentNinja", "silent ninja")]
    [InlineData("avianaria_lilium", "avian aria lilium")]
    [InlineData("Drakcain", "drakcain")]
    public void UsernameSpeechNormalizerKeepsIdentityButImprovesSafeStructure(string raw, string expected)
    {
        Assert.Equal(expected, UsernameSpeechNormalizer.Normalize(raw));
    }

    [Fact]
    public void ReadUsernamesUsesActualChatterAndCanBeDisabled()
    {
        var chat = new ChatEvent("name-on", "ViewerDisplay", "hello everyone", DateTimeOffset.UtcNow);
        var withNames = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), withNames, new EventDeduplicator()).Accept(chat));
        Assert.Equal("viewer display said: hello everyone", withNames.Take(DateTimeOffset.UtcNow)!.Text);
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

    [Fact]
    public void InvisibleDisplayNameFallsBackToTwitchLoginAndNeverProducesABareSaidPrefix()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"invisible-name"},"payload":{"event":{"chatter_user_name":"\u200B","chatter_user_login":"real_viewer","message":{"text":"hello"}}}}""";
        var chat = EventSubParser.Chat(json, DateTimeOffset.UtcNow)!;
        Assert.Equal("real_viewer", chat.Chatter);
        var queue = new FreshQueue();
        Assert.True(new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator()).Accept(chat));
        Assert.Equal("real viewer said: hello", queue.Take(DateTimeOffset.UtcNow)!.Text);
    }
}
