using ChatVox.Runtime;

namespace ChatVox.Settings;

public static class StartupVisibilityPolicy
{
    public static bool ShouldStartHidden(AppSettings settings, LaunchReason reason) =>
        reason is LaunchReason.Normal or LaunchReason.WindowsStartup &&
        settings.StartMinimizedToTray && settings.StartMinimizedWasExplicitlySet;
}
