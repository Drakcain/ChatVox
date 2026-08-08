using System.IO;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Microsoft.Win32;
using ChatVox.Settings;

namespace ChatVox;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        SetTheme(AppearanceMode.Dark);
        base.OnStartup(e);
        SessionEnding += (_, _) =>
        {
            if (MainWindow is MainWindow window) window.PrepareForSystemShutdown();
        };
    }

    public static void SetTheme(AppearanceMode requested)
    {
        var light = requested == AppearanceMode.Light || (requested == AppearanceMode.System && WindowsUsesLightTheme());
        Current.Resources["WindowBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#F7F8FC" : "#111214"));
        Current.Resources["CardBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#FFFFFF" : "#1E1F22"));
        Current.Resources["ControlBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#EDF0F7" : "#2B2D31"));
        Current.Resources["InputBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#FFFFFF" : "#131416"));
        Current.Resources["ForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#1A2433" : "#F2F5FA"));
        Current.Resources["MutedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#596579" : "#B7C1D1"));
        Current.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#D3DAE6" : "#303136"));
        Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D5EF7"));
    }

    private static bool WindowsUsesLightTheme()
    {
        try { return (int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) == 1; }
        catch { return false; }
    }
}
