using ChatVox.Filtering;
using ChatVox.Queue;
using ChatVox.Settings;
using ChatVox.Twitch;
using ChatVox.Updates;
using System.Net;
using System.Security.Cryptography;

namespace ChatVox.Tests;

public sealed class Rc3UxTests
{
    [Theory]
    [InlineData("hello 😂😂😂", "hello")]
    [InlineData("good game 👍🏽 thanks", "good game thanks")]
    [InlineData("hello 👨‍👩‍👧‍👦 there", "hello there")]
    [InlineData("flag 🇺🇸 and key 1️⃣", "flag and key")]
    [InlineData("I ❤️ ChatVox", "I ChatVox")]
    [InlineData("time: 8:30; café déjà vu", "time: 8:30; café déjà vu")]
    public void UnicodeEmojiAreRemovedWhileWordingIsPreserved(string input, string expected) => Assert.Equal(expected, ChatTextSanitizer.Normalize(input));

    [Fact]
    public void StructuredTwitchEmotesAreRemovedWithoutSpeakingIdentifiers()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"emote-1"},"payload":{"event":{"chatter_user_name":"Viewer","message":{"text":"hello Kappa :hand:","fragments":[{"text":"hello ","type":"text"},{"text":"Kappa","type":"emote","emote":{"id":"1"}},{"text":" ","type":"text"},{"text":":hand:","type":"emote","emote":{"id":"2"}}]}}}}""";
        Assert.Equal("hello", ChatTextSanitizer.Normalize(EventSubParser.Chat(json, DateTimeOffset.UtcNow)!.Text));
    }

    [Fact]
    public void EmoteOnlyMessageIsDroppedBeforeUsernameCanBeQueued()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"emote-only"},"payload":{"event":{"chatter_user_name":"Viewer","message":{"text":"Kappa","fragments":[{"text":"Kappa","type":"emote","emote":{"id":"1"}}]}}}}""";
        var queue = new FreshQueue(); var pipeline = new ChatPipeline(new ChatFilter(), queue, new EventDeduplicator());
        Assert.False(pipeline.Accept(EventSubParser.Chat(json, DateTimeOffset.UtcNow)!));
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void NormalColonTextIsPreservedWhenTwitchDoesNotMarkItAsAnEmote()
    {
        const string json = """{"metadata":{"message_type":"notification","message_id":"colon"},"payload":{"event":{"chatter_user_name":"Viewer","message":{"text":"note: hello at 8:30","fragments":[{"text":"note: hello at 8:30","type":"text"}]}}}}""";
        Assert.Equal("note: hello at 8:30", EventSubParser.Chat(json, DateTimeOffset.UtcNow)!.Text);
    }

    [Fact]
    public void NewConsumerPreferencesPersistWithExistingSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = new AppSettingsStore(path);
        store.Save(new AppSettings { Voice = "af_bella", IgnoredUsers = ["MyBot"], StartMinimizedToTray = true, AutomaticallyCheckForUpdates = false });
        var saved = store.Load();
        Assert.Equal("af_bella", saved.Voice); Assert.Equal(["MyBot"], saved.IgnoredUsers);
        Assert.True(saved.StartMinimizedToTray); Assert.False(saved.AutomaticallyCheckForUpdates);
        File.Delete(path);
    }

    [Fact]
    public void WindowsStartupCommandIsQuotedAndUsesTheExecutablePath()
    {
        Assert.Equal("\"C:\\Program Files\\ChatVox\\ChatVox.exe\" --windows-startup", WindowsStartupService.CurrentCommand(@"C:\Program Files\ChatVox\ChatVox.exe"));
    }

    [Fact]
    public void ReleaseCandidateVersionOrderingIsCorrect()
    {
        Assert.True(UpdateService.CompareVersions("1.0.0-rc.4", "1.0.0-rc.3") > 0);
        Assert.True(UpdateService.CompareVersions("1.0.0", "1.0.0-rc.4") > 0);
        Assert.Equal(0, UpdateService.CompareVersions("1.0.0-rc.5+build.123", "1.0.0-rc.5"));
        Assert.Equal(0, UpdateService.CompareVersions("1.0.0-rc.3", "1.0.0-rc.3"));
    }

    [Fact]
    public async Task UpdateFeedFailureIsHarmless()
    {
        using var http = new HttpClient(new FailingHandler());
        var result = await new UpdateService(http).CheckAsync("1.0.0-rc.5", CancellationToken.None);
        Assert.True(result.IsConfigured); Assert.False(result.IsUpdateAvailable); Assert.Contains("Unable", result.SafeMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LatestValidPrereleaseIsDiscoveredWithoutAChannelSelector()
    {
        const string feed = """
        [
          {"draft":false,"prerelease":true,"tag_name":"v1.0.0-rc.8","assets":[{"name":"ChatVox-1.0.0-rc.8-Setup.exe","browser_download_url":"https://unit.test/ChatVox-1.0.0-rc.8-Setup.exe","size":5},{"name":"ChatVox-1.0.0-rc.8-Setup.exe.sha256","browser_download_url":"https://unit.test/ChatVox-1.0.0-rc.8-Setup.exe.sha256","size":90}]},
          {"draft":false,"prerelease":false,"tag_name":"v1.0.0-rc.7","assets":[{"name":"ChatVox-1.0.0-rc.7-Setup.exe","browser_download_url":"https://unit.test/ChatVox-1.0.0-rc.7-Setup.exe","size":5},{"name":"ChatVox-1.0.0-rc.7-Setup.exe.sha256","browser_download_url":"https://unit.test/ChatVox-1.0.0-rc.7-Setup.exe.sha256","size":90}]}
        ]
        """;
        using var http = new HttpClient(new StaticHandler(feed));
        var result = await new UpdateService(http).CheckAsync("1.0.0-rc.7", CancellationToken.None);
        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("1.0.0-rc.8", result.AvailableVersion);
    }

    [Fact]
    public async Task VerifiedInstallerCanBeRenamedAfterChecksumVerification()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var hash = Convert.ToHexString(SHA256.HashData(payload));
        using var http = new HttpClient(new DownloadHandler(payload, hash));
        var update = new UpdateCheckResult(true, true, "update", "1.0.0-rc.6", new Uri("https://unit.test/ChatVox-1.0.0-rc.6-Setup.exe"), new Uri("https://unit.test/ChatVox-1.0.0-rc.6-Setup.exe.sha256"), payload.Length);
        var package = await new UpdateService(http, root).DownloadAndVerifyAsync(update, null, CancellationToken.None);
        Assert.True(File.Exists(package.InstallerPath));
        Assert.Equal(payload, await File.ReadAllBytesAsync(package.InstallerPath));
        Assert.False(File.Exists(package.InstallerPath + ".partial"));
        Directory.Delete(root, true);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new HttpRequestException("offline");
    }

    private sealed class DownloadHandler(byte[] payload, string hash) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.Ordinal)
                ? new StringContent($"{hash}  ChatVox-1.0.0-rc.6-Setup.exe")
                : new ByteArrayContent(payload);
            if (content is ByteArrayContent) content.Headers.ContentLength = payload.Length;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class StaticHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }
}
