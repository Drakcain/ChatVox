using System.Reflection;
using System.Text.Json;
using ChatVox.Filtering;
using ChatVox.Queue;
using ChatVox.Settings;
using ChatVox.Speech;
using ChatVox.Twitch;
using KokoroSharp;

namespace ChatVox.Tests;

public class CoreTests
{
    [Fact]
    public void ReleaseCandidateVersionIsAuthoritative()
    {
        Assert.StartsWith("1.0.0-rc.8", typeof(Voices).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    }

    [Fact]
    public void CatalogContainsOnlySupportedEnglishVoicesWithMetadataAndAsset()
    {
        Assert.Equal(Voices.ExpectedEnglishVoiceCount, Voices.All.Count);
        Assert.Equal(Voices.All.Count, Voices.All.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(Voices.All, voice =>
        {
            Assert.False(string.IsNullOrWhiteSpace(voice.FriendlyName));
            Assert.False(string.IsNullOrWhiteSpace(voice.Category));
            Assert.True(File.Exists(voice.AssetPath), $"Missing asset for {voice.Id}");
            Assert.DoesNotContain("_", voice.DisplayName, StringComparison.Ordinal);
            Assert.Matches("^(af|am|bf|bm)_", voice.Id);
        });
    }

    [Fact]
    public void SupportedVoiceAssetsLoadThroughCurrentKokoroRuntime()
    {
        Assert.All(Voices.All, voice => Assert.NotNull(KokoroVoiceManager.GetVoice(voice.Id)));
    }

    [Fact]
    public void BundledKokoroModelLoadsFromTheLocalApplicationDirectory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "kokoro-fp16.onnx");
        Assert.True(File.Exists(path));
        using var model = KokoroTTS.LoadModel(path, null);
        Assert.NotNull(model);
    }

    [Fact]
    public void CatalogOrdersByCategoryThenFriendlyName()
    {
        Assert.Equal(Voices.All.OrderBy(x => x.CategoryOrder).ThenBy(x => x.WithinCategoryOrder).ThenBy(x => x.FriendlyName, StringComparer.OrdinalIgnoreCase).Select(x => x.Id), Voices.All.Select(x => x.Id));
        Assert.Equal("Default", Voices.All.First().FriendlyName);
        Assert.Equal("American Female", Voices.All.First().Category);
    }

    [Fact]
    public void CatalogDoesNotExposeUnsupportedLanguageCategories()
    {
        Assert.All(Voices.All, voice => Assert.Contains(voice.Category, new[] { "American Female", "American Male", "British Female", "British Male" }));
        Assert.DoesNotContain(Voices.All, voice => voice.Id.StartsWith("ef_", StringComparison.OrdinalIgnoreCase) || voice.Id.StartsWith("em_", StringComparison.OrdinalIgnoreCase) || voice.Id.StartsWith("zf_", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VoicesResolveAndPersistByInternalId()
    {
        Assert.Equal("af_heart", Voices.Default);
        Assert.All(Voices.All, voice => Assert.Equal(voice.Id, Voices.Resolve(voice)));
        Assert.Equal("af_heart", Voices.Resolve("Default (American Female)"));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = new AppSettingsStore(path);
        store.Save(new AppSettings { Appearance = AppearanceMode.Light, Speed = 1.4, Voice = "af_bella" });
        var saved = store.Load();
        Assert.Equal(AppearanceMode.Light, saved.Appearance);
        Assert.Equal(1.4, saved.Speed);
        Assert.Equal("af_bella", saved.Voice);
        Assert.Equal("Bella (American Female)", Voices.LabelFor(saved.Voice));
        File.Delete(path);
    }

    [Fact]
    public void VoiceAssetCacheCopiesBundledAssetsToUserWritableRuntimeLocation()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "installed", "voices");
        var cache = Path.Combine(root, "localappdata", "ChatVox", "speech", "voices");
        Directory.CreateDirectory(source);
        var sourceVoice = Path.Combine(source, "af_heart.npy");
        File.WriteAllBytes(sourceVoice, [1, 2, 3, 4]);
        var result = VoiceAssetCache.Ensure(source, cache);
        Assert.Equal(1, result.RefreshedAssets);
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(Path.Combine(cache, "af_heart.npy")));
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(sourceVoice));
        Directory.Delete(root, true);
    }

    [Fact]
    public void VoiceAssetCacheRepairsCorruptionAndRemovesOnlyObsoleteManagedAssets()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "installed", "voices");
        var cache = Path.Combine(root, "localappdata", "ChatVox", "speech", "voices");
        Directory.CreateDirectory(source); Directory.CreateDirectory(cache);
        File.WriteAllBytes(Path.Combine(source, "af_heart.npy"), [1, 2, 3, 4]);
        File.WriteAllBytes(Path.Combine(cache, "af_heart.npy"), [9, 9, 9, 9]);
        File.WriteAllBytes(Path.Combine(cache, "obsolete_voice.npy"), [1]);
        var result = VoiceAssetCache.Ensure(source, cache);
        Assert.Equal(1, result.RefreshedAssets);
        Assert.Equal(1, result.RemovedObsoleteAssets);
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(Path.Combine(cache, "af_heart.npy")));
        Assert.False(File.Exists(Path.Combine(cache, "obsolete_voice.npy")));
        Directory.Delete(root, true);
    }

    [Fact]
    public void SettingsNormalizeRemovedVoiceAndPreserveOtherSettings()
    {
        var s = new AppSettings { Appearance = (AppearanceMode)99, Voice = "ef_dora", Speed = 1.4, IgnoredUsers = [" NightBot ", "nightbot", "", "MyBot"] };
        s.Normalize();
        Assert.Equal(AppearanceMode.Dark, s.Appearance);
        Assert.Equal(Voices.Default, s.Voice);
        Assert.Equal(1.4, s.Speed);
        Assert.Equal(["NightBot", "MyBot"], s.IgnoredUsers);
    }

    [Fact]
    public void EnglishPhonemizerHandlesChatCorpusWithoutEspeak()
    {
        var corpus = new[] { "Hello everyone", "Thanks for watching", "That was amazing", "don't can't we're they've I'd", "123 2026 8:30 $19.99 100%", "hello! really? well... test: one, two", "Drakcain ChatVox Twitch Kokoro", "café résumé naïve jalapeño Pokémon", "@ # & + -", "\u201cMixed capitalization!!!\u201d", "This is a longer normal chat sentence with punctuation, names, and numbers so the queue can process a realistic message without requiring an external phonemizer." };
        foreach (var text in corpus)
        {
            var american = EnglishPhonemizer.Phonemize(text, british: false);
            var british = EnglishPhonemizer.Phonemize(text, british: true);
            Assert.False(string.IsNullOrWhiteSpace(american.NormalizedText));
            Assert.NotEmpty(american.Tokens);
            Assert.NotEmpty(british.Tokens);
        }
    }

    [Fact]
    public void EnglishPhonemizerNormalizesBorrowedWordsAndUnicodeQuotes()
    {
        Assert.Equal("\"cafe resume naive jalapeno Pokemon\"", EnglishPhonemizer.Normalize("\u201ccafé résumé naïve jalapeño Pokémon\u201d"));
    }

    [Fact] public void Filters(){var f=new ChatFilter(["bad"]);Assert.False(f.Accept("a","!x"));Assert.False(f.Accept("a","https://x"));Assert.False(f.Accept("bad","hello"));Assert.True(f.Accept("a","hello"));Assert.True(f.Accept("a","hello"));}
    [Fact] public void DefaultAutomationAccountsAreIgnoredAndEditable(){var f=new ChatFilter(AppSettings.DefaultIgnoredUsers);Assert.False(f.Accept("Nightbot","normal message"));f.SetIgnoredUsers(["custombot"]);Assert.True(f.Accept("Nightbot","normal message"));Assert.False(f.Accept("custombot","normal message"));}
    [Fact] public void LongMessagesAreBoundedAndMultipleChattersAreAccepted(){var q=new FreshQueue();var p=new ChatPipeline(new ChatFilter(),q,new EventDeduplicator()){MaxMessageLength=200,ReadUsernames=false};Assert.True(p.Accept(new ChatEvent("viewer-a","Alice",new string('a',200),DateTimeOffset.UtcNow)));Assert.True(p.Accept(new ChatEvent("viewer-b","Bob","hello",DateTimeOffset.UtcNow)));Assert.False(p.Accept(new ChatEvent("long","Alice",new string('x',201),DateTimeOffset.UtcNow)));Assert.Equal(2,q.Count);}
    [Fact] public void Queue(){var q=new FreshQueue(2,TimeSpan.FromSeconds(10));q.Add("a",DateTimeOffset.UnixEpoch);q.Add("b",DateTimeOffset.UnixEpoch.AddSeconds(1));q.Add("c",DateTimeOffset.UnixEpoch.AddSeconds(2));Assert.Equal("b",q.Take(DateTimeOffset.UnixEpoch.AddSeconds(2))!.Text);Assert.Null(q.Take(DateTimeOffset.UnixEpoch.AddSeconds(20)));}
    [Fact] public void QueueReportsExpiry(){var q=new FreshQueue(2,TimeSpan.FromSeconds(1));var expired=0;q.Expired+=count=>expired+=count;q.Add("a",DateTimeOffset.UnixEpoch);q.Purge(DateTimeOffset.UnixEpoch.AddSeconds(2));Assert.Equal(1,expired);}
    [Fact] public void DeviceCodeParses(){var d=JsonSerializer.Deserialize<DeviceCodeResponse>("{\"device_code\":\"x\",\"user_code\":\"ABCD\",\"verification_uri\":\"https://example.test\",\"expires_in\":60,\"interval\":5}")!;Assert.Equal("ABCD",d.UserCode);}
    [Fact] public void TokenValidationParsesTwitchUnderscoreFields(){var value=JsonSerializer.Deserialize<TokenValidation>("{\"client_id\":\"app\",\"user_id\":\"broadcaster\",\"expires_in\":3600}")!;Assert.Equal("app",value.ClientId);Assert.Equal("broadcaster",value.UserId);}
    [Fact] public void DedupExpires(){var d=new EventDeduplicator(TimeSpan.FromSeconds(10),2);var n=DateTimeOffset.UnixEpoch;Assert.True(d.First("a",n));Assert.False(d.First("a",n));Assert.True(d.First("a",n.AddSeconds(11)));}
    [Fact] public void ReconnectUrlParses(){const string json="{\"payload\":{\"session\":{\"reconnect_url\":\"wss://example.test/reconnect\"}}}";Assert.Equal("wss://example.test/reconnect",EventSubParser.ReconnectUrl(json));}
    [Fact] public void DpapiStoreRoundTripAndReset(){var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".bin");var store=new DpapiAuthStore(path);store.Save(new TokenResponse("fake-access","fake-refresh",60));Assert.Equal("fake-access",store.Load()!.AccessToken);store.Clear();Assert.Null(store.Load());}
    [Fact] public void DpapiCorruptionFallsBack(){var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".bin");File.WriteAllBytes(path,[1,2,3]);Assert.Null(new DpapiAuthStore(path).Load());File.Delete(path);}
    [Fact] public void RetryBackoffIsBounded(){Assert.Equal(TimeSpan.FromSeconds(1),RetryPolicy.Delay(0));Assert.Equal(TimeSpan.FromSeconds(30),RetryPolicy.Delay(99));}
    [Fact] public void MockEventReachesQueueOnce(){const string json="{\"metadata\":{\"message_type\":\"notification\",\"message_id\":\"test-event-001\"},\"payload\":{\"event\":{\"chatter_user_name\":\"TestViewer\",\"message\":{\"text\":\"hello ChatVox\"}}}}";var e=EventSubParser.Chat(json,DateTimeOffset.UnixEpoch)!;var q=new FreshQueue();var p=new ChatPipeline(new ChatFilter(),q,new EventDeduplicator());Assert.True(p.Accept(e));Assert.False(p.Accept(e));Assert.Equal("TestViewer said: hello ChatVox",q.Take(DateTimeOffset.UnixEpoch)!.Text);}
    [Fact] public void BurstNotificationsAllReachQueue(){var q=new FreshQueue(6,TimeSpan.FromMinutes(1));var p=new ChatPipeline(new ChatFilter(),q,new EventDeduplicator());for(var i=1;i<=5;i++){var json=$"{{\"metadata\":{{\"message_type\":\"notification\",\"message_id\":\"burst-{i}\"}},\"payload\":{{\"event\":{{\"chatter_user_name\":\"Viewer\",\"message\":{{\"text\":\"{i}\"}}}}}}}}";Assert.True(p.Accept(EventSubParser.Chat(json,DateTimeOffset.UtcNow)!));}Assert.Equal(5,q.Count);}
}
