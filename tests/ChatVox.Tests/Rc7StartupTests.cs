using System.Text.Json;
using ChatVox.Settings;

namespace ChatVox.Tests;

public sealed class Rc7StartupTests
{
    [Fact]
    public void FreshSettingsStartVisibleByDefault()
    {
        var settings = new AppSettings();
        Assert.False(settings.StartMinimizedToTray);
        Assert.False(StartupVisibilityPolicy.ShouldStartHidden(settings));
    }

    [Fact]
    public void ExplicitStartMinimizedPreferenceIsPreserved()
    {
        var settings = new AppSettings { StartMinimizedToTray = true, StartMinimizedWasExplicitlySet = true };
        Assert.True(StartupVisibilityPolicy.ShouldStartHidden(settings));
    }

    [Fact]
    public void Rc6SettingsDeserializeWithoutTheRemovedUpdateChannelBeingRequired()
    {
        const string json = """{"Voice":"af_bella","IgnoreUrls":false,"StartMinimizedToTray":true,"UpdateChannel":1}""";
        var settings = JsonSerializer.Deserialize<AppSettings>(json)!;
        settings.Normalize();
        Assert.Equal("af_bella", settings.Voice);
        Assert.False(settings.IgnoreUrls);
        Assert.True(settings.StartMinimizedWasExplicitlySet);
        Assert.True(settings.IgnoreEmoji);
    }
}
