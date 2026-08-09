using ChatVox.Windowing;

namespace ChatVox.Tests;

public sealed class WindowGeometryTests
{
    [Fact]
    public void InitialPlacementUsesWorkAreaAndCenters()
    {
        var result = WindowGeometry.Initial(new WorkArea(0, 0, 1920, 1040));
        Assert.Equal(1400, result.Width);
        Assert.Equal(884, result.Height);
        Assert.Equal(260, result.Left);
        Assert.Equal(78, result.Top);
    }

    [Fact]
    public void InitialPlacementFitsSmallWorkArea()
    {
        var result = WindowGeometry.Initial(new WorkArea(0, 0, 1366, 728));
        Assert.InRange(result.Width, WindowGeometry.MinimumWidth, 1366);
        Assert.InRange(result.Height, WindowGeometry.MinimumHeight, 728);
        Assert.True(result.Right <= 1366);
        Assert.True(result.Bottom <= 728);
    }

    [Fact]
    public void InitialPlacementCapsUltrawideWidthWithoutRestrictingManualResize()
    {
        var result = WindowGeometry.Initial(new WorkArea(0, 0, 3440, 1400));
        Assert.Equal(WindowGeometry.MaximumInitialWidth, result.Width);
        Assert.Equal((3440 - WindowGeometry.MaximumInitialWidth) / 2, result.Left);
        Assert.True(result.Height <= 1400);
    }

    [Fact]
    public void RestoreClampsOversizedSavedBoundsToTheActiveWorkArea()
    {
        var result = WindowGeometry.Restore(new WindowBounds(-500, -300, 4000, 3000), [new WorkArea(0, 0, 1920, 1040)]);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1040, result.Height);
        Assert.Equal(0, result.Left);
        Assert.Equal(0, result.Top);
    }

    [Fact]
    public void RestorePreservesAValidSecondaryMonitorPlacement()
    {
        var result = WindowGeometry.Restore(new WindowBounds(2200, 100, 1000, 800), [new WorkArea(0, 0, 1920, 1040), new WorkArea(1920, 0, 1920, 1040)]);
        Assert.Equal(2200, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(1000, result.Width);
        Assert.Equal(800, result.Height);
    }

    [Fact]
    public void RestoreMovesDisconnectedMonitorBoundsBackToPrimary()
    {
        var result = WindowGeometry.Restore(new WindowBounds(4000, 100, 1000, 800), [new WorkArea(0, 0, 1920, 1040)]);
        Assert.InRange(result.Left, 0, 920);
        Assert.InRange(result.Top, 0, 240);
        Assert.True(result.Right <= 1920);
        Assert.True(result.Bottom <= 1040);
    }

    [Fact]
    public void RestoreWithoutSavedBoundsUsesSmartPrimaryPlacement()
    {
        var result = WindowGeometry.Restore(null, [new WorkArea(0, 0, 1920, 1040)]);
        Assert.Equal(WindowGeometry.Initial(new WorkArea(0, 0, 1920, 1040)), result);
    }

    [Fact]
    public void NewWindowSettingsRemainBackwardCompatible()
    {
        var settings = new ChatVox.Settings.AppSettings();
        settings.Normalize();
        Assert.Null(settings.WindowLeft);
        Assert.Null(settings.WindowTop);
        Assert.Null(settings.WindowWidth);
        Assert.Null(settings.WindowHeight);
        Assert.False(settings.WindowWasMaximized);
    }

    [Fact]
    public void WindowPlacementPersistsWithoutResettingExistingSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = new ChatVox.Settings.AppSettingsStore(path);
        store.Save(new ChatVox.Settings.AppSettings
        {
            Voice = "af_bella",
            WindowLeft = 200,
            WindowTop = 100,
            WindowWidth = 1100,
            WindowHeight = 800,
            WindowWasMaximized = true
        });
        var restored = store.Load();
        Assert.Equal("af_bella", restored.Voice);
        Assert.Equal(200, restored.WindowLeft);
        Assert.Equal(100, restored.WindowTop);
        Assert.Equal(1100, restored.WindowWidth);
        Assert.Equal(800, restored.WindowHeight);
        Assert.True(restored.WindowWasMaximized);
        File.Delete(path);
    }
}
