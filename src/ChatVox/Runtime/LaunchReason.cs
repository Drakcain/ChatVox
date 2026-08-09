namespace ChatVox.Runtime;

public enum LaunchReason
{
    Normal,
    WindowsStartup,
    PostInstall,
    PostUpdate,
    SecondaryActivation
}

public static class LaunchReasonParser
{
    public static LaunchReason FromArguments(IEnumerable<string> arguments)
    {
        var values = arguments.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (values.Contains("--post-update")) return LaunchReason.PostUpdate;
        if (values.Contains("--post-install")) return LaunchReason.PostInstall;
        return values.Contains("--windows-startup") ? LaunchReason.WindowsStartup : LaunchReason.Normal;
    }
}
