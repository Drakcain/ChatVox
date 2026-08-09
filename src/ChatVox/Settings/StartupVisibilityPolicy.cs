namespace ChatVox.Settings;

public static class StartupVisibilityPolicy
{
    public static bool ShouldStartHidden(AppSettings settings) => settings.StartMinimizedToTray && settings.StartMinimizedWasExplicitlySet;
}
