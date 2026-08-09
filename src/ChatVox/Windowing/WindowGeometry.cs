namespace ChatVox.Windowing;

/// <summary>Pure, DPI-logical geometry used to keep the main window reachable.</summary>
public readonly record struct WorkArea(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
}

public readonly record struct WindowBounds(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
}

public static class WindowGeometry
{
    public const double MinimumWidth = 760;
    public const double MinimumHeight = 600;
    public const double MaximumInitialWidth = 1400;

    public static WindowBounds Initial(WorkArea workArea)
    {
        var width = Math.Min(ClampDimension(workArea.Width * 0.75, workArea.Width, MinimumWidth), MaximumInitialWidth);
        var height = ClampDimension(workArea.Height * 0.85, workArea.Height, MinimumHeight);
        return new WindowBounds(
            workArea.Left + (workArea.Width - width) / 2,
            workArea.Top + (workArea.Height - height) / 2,
            width,
            height);
    }

    public static WindowBounds Restore(WindowBounds? saved, IEnumerable<WorkArea> activeWorkAreas)
    {
        var areas = activeWorkAreas.Where(IsUsable).ToArray();
        var primary = areas.FirstOrDefault();
        if (!IsUsable(primary)) primary = new WorkArea(0, 0, 1920, 1040);
        if (saved is null || saved.Value.Width <= 0 || saved.Value.Height <= 0) return Initial(primary);

        var source = saved.Value;
        var destination = areas
            .OrderByDescending(area => IntersectionArea(source, area))
            .FirstOrDefault();
        if (!IsUsable(destination) || IntersectionArea(source, destination) <= 0) destination = primary;

        var width = ClampDimension(source.Width, destination.Width, MinimumWidth);
        var height = ClampDimension(source.Height, destination.Height, MinimumHeight);
        var left = Math.Clamp(source.Left, destination.Left, destination.Right - width);
        var top = Math.Clamp(source.Top, destination.Top, destination.Bottom - height);
        return new WindowBounds(left, top, width, height);
    }

    private static bool IsUsable(WorkArea area) => area.Width > 0 && area.Height > 0;
    private static double ClampDimension(double value, double available, double minimum) => Math.Min(Math.Max(value, Math.Min(minimum, available)), available);
    private static double IntersectionArea(WindowBounds window, WorkArea area)
    {
        var width = Math.Max(0, Math.Min(window.Right, area.Right) - Math.Max(window.Left, area.Left));
        var height = Math.Max(0, Math.Min(window.Bottom, area.Bottom) - Math.Max(window.Top, area.Top));
        return width * height;
    }
}
