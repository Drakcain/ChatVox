using System.Text.Json;
using ChatVox.Runtime;
using ChatVox.Settings;

namespace ChatVox.Tests;

public sealed class Rc7StartupTests
{
    [Fact]
    public void FreshSettingsStartVisibleByDefault()
    {
        var settings = new AppSettings();
        Assert.False(settings.StartMinimizedToTray);
        Assert.False(StartupVisibilityPolicy.ShouldStartHidden(settings, LaunchReason.Normal));
    }

    [Fact]
    public void ExplicitStartMinimizedPreferenceIsPreserved()
    {
        var settings = new AppSettings { StartMinimizedToTray = true, StartMinimizedWasExplicitlySet = true };
        Assert.True(StartupVisibilityPolicy.ShouldStartHidden(settings, LaunchReason.Normal));
    }

    [Theory]
    [InlineData(LaunchReason.Normal, false, false)]
    [InlineData(LaunchReason.Normal, true, true)]
    [InlineData(LaunchReason.WindowsStartup, false, false)]
    [InlineData(LaunchReason.WindowsStartup, true, true)]
    [InlineData(LaunchReason.PostInstall, true, false)]
    [InlineData(LaunchReason.PostUpdate, true, false)]
    [InlineData(LaunchReason.SecondaryActivation, true, false)]
    public void LaunchReasonControlsStartupVisibility(LaunchReason reason, bool startMinimized, bool expectedHidden)
    {
        var settings = new AppSettings
        {
            StartMinimizedToTray = startMinimized,
            StartMinimizedWasExplicitlySet = startMinimized
        };

        Assert.Equal(expectedHidden, StartupVisibilityPolicy.ShouldStartHidden(settings, reason));
    }

    [Fact]
    public void LaunchReasonParserRecognizesExplicitContexts()
    {
        Assert.Equal(LaunchReason.Normal, LaunchReasonParser.FromArguments([]));
        Assert.Equal(LaunchReason.WindowsStartup, LaunchReasonParser.FromArguments(["--windows-startup"]));
        Assert.Equal(LaunchReason.PostInstall, LaunchReasonParser.FromArguments(["--post-install"]));
        Assert.Equal(LaunchReason.PostUpdate, LaunchReasonParser.FromArguments(["--post-update"]));
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
