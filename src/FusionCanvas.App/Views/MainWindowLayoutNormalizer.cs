using Avalonia;
using Avalonia.Controls;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Views;

internal sealed record ScreenLayoutInfo(PixelRect WorkingArea, double Scaling, bool IsPrimary);

internal static class MainWindowLayoutNormalizer
{
    internal const double NavigationMinimumWidth = 240;
    internal const double NavigationMaximumWidth = 560;
    private const int MinimumVisiblePixels = 48;

    public static bool TryCapture(
        WindowState state,
        PixelPoint position,
        double width,
        double height,
        double navigationWidth,
        double minimumWindowWidth,
        double minimumWindowHeight,
        out WindowLayoutSettings layout)
    {
        layout = default!;
        if (state != WindowState.Normal ||
            !IsFinitePositive(width) ||
            !IsFinitePositive(height) ||
            width < minimumWindowWidth ||
            height < minimumWindowHeight ||
            !IsFinitePositive(navigationWidth) ||
            navigationWidth < NavigationMinimumWidth ||
            navigationWidth > NavigationMaximumWidth)
        {
            return false;
        }

        layout = new WindowLayoutSettings(
            position.X,
            position.Y,
            width,
            height,
            navigationWidth);
        return true;
    }

    public static bool TryNormalize(
        WindowLayoutSettings saved,
        IReadOnlyList<ScreenLayoutInfo> screens,
        double minimumWindowWidth,
        double minimumWindowHeight,
        out WindowLayoutSettings normalized)
    {
        normalized = default!;
        if (screens.Count == 0 ||
            !IsFinitePositive(saved.Width) ||
            !IsFinitePositive(saved.Height) ||
            !IsFinitePositive(saved.NavigationWidth) ||
            saved.Width < minimumWindowWidth ||
            saved.Height < minimumWindowHeight ||
            saved.NavigationWidth < NavigationMinimumWidth ||
            saved.NavigationWidth > NavigationMaximumWidth)
        {
            return false;
        }

        if (!TryNormalizeBounds(
            saved.PositionX, saved.PositionY, saved.Width, saved.Height,
            screens, minimumWindowWidth, minimumWindowHeight,
            out var positionX, out var positionY, out var width, out var height))
        {
            return false;
        }

        normalized = new WindowLayoutSettings(positionX, positionY, width, height, saved.NavigationWidth);
        return true;
    }

    public static bool TryCaptureGeometry(
        WindowState state,
        PixelPoint position,
        double width,
        double height,
        double minimumWindowWidth,
        double minimumWindowHeight,
        out WindowGeometrySettings geometry)
    {
        geometry = default!;
        if (state != WindowState.Normal ||
            !IsFinitePositive(width) ||
            !IsFinitePositive(height) ||
            width < minimumWindowWidth ||
            height < minimumWindowHeight)
        {
            return false;
        }

        geometry = new WindowGeometrySettings(position.X, position.Y, width, height);
        return true;
    }

    public static bool TryNormalizeGeometry(
        WindowGeometrySettings saved,
        IReadOnlyList<ScreenLayoutInfo> screens,
        double minimumWindowWidth,
        double minimumWindowHeight,
        out WindowGeometrySettings normalized)
    {
        normalized = default!;
        if (screens.Count == 0 ||
            !IsFinitePositive(saved.Width) ||
            !IsFinitePositive(saved.Height) ||
            saved.Width < minimumWindowWidth ||
            saved.Height < minimumWindowHeight)
        {
            return false;
        }

        if (!TryNormalizeBounds(
            saved.PositionX, saved.PositionY, saved.Width, saved.Height,
            screens, minimumWindowWidth, minimumWindowHeight,
            out var positionX, out var positionY, out var width, out var height))
        {
            return false;
        }

        normalized = new WindowGeometrySettings(positionX, positionY, width, height);
        return true;
    }

    private static bool TryNormalizeBounds(
        int savedX,
        int savedY,
        double savedWidth,
        double savedHeight,
        IReadOnlyList<ScreenLayoutInfo> screens,
        double minimumWindowWidth,
        double minimumWindowHeight,
        out int positionX,
        out int positionY,
        out double width,
        out double height)
    {
        positionX = 0;
        positionY = 0;
        width = 0;
        height = 0;

        var screen = SelectScreen(savedX, savedY, screens);
        if (!double.IsFinite(screen.Scaling) || screen.Scaling <= 0 ||
            screen.WorkingArea.Width <= 0 || screen.WorkingArea.Height <= 0)
        {
            return false;
        }

        var maximumWidth = screen.WorkingArea.Width / screen.Scaling;
        var maximumHeight = screen.WorkingArea.Height / screen.Scaling;
        if (maximumWidth < minimumWindowWidth || maximumHeight < minimumWindowHeight)
        {
            return false;
        }

        width = Math.Min(savedWidth, maximumWidth);
        height = Math.Min(savedHeight, maximumHeight);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * screen.Scaling));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * screen.Scaling));
        var visiblePixels = Math.Min(MinimumVisiblePixels, Math.Min(pixelWidth, pixelHeight));

        var minimumX = screen.WorkingArea.X - pixelWidth + visiblePixels;
        var maximumX = screen.WorkingArea.X + screen.WorkingArea.Width - visiblePixels;
        var minimumY = screen.WorkingArea.Y - pixelHeight + visiblePixels;
        var maximumY = screen.WorkingArea.Y + screen.WorkingArea.Height - visiblePixels;

        positionX = Clamp(savedX, minimumX, maximumX);
        positionY = Clamp(savedY, minimumY, maximumY);
        return true;
    }

    private static ScreenLayoutInfo SelectScreen(
        int x,
        int y,
        IReadOnlyList<ScreenLayoutInfo> screens)
    {
        foreach (var screen in screens)
        {
            if (Contains(screen.WorkingArea, x, y))
            {
                return screen;
            }
        }

        return screens
            .OrderBy(screen => DistanceSquared(screen.WorkingArea, x, y))
            .ThenByDescending(screen => screen.IsPrimary)
            .First();
    }

    private static bool Contains(PixelRect area, int x, int y) =>
        x >= area.X && x < area.X + area.Width &&
        y >= area.Y && y < area.Y + area.Height;

    private static long DistanceSquared(PixelRect area, int x, int y)
    {
        var dx = x < area.X ? area.X - x : x >= area.X + area.Width ? x - (area.X + area.Width - 1) : 0;
        var dy = y < area.Y ? area.Y - y : y >= area.Y + area.Height ? y - (area.Y + area.Height - 1) : 0;
        return (long)dx * dx + (long)dy * dy;
    }

    private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0;

    private static int Clamp(int value, int minimum, int maximum) =>
        minimum > maximum ? minimum : Math.Clamp(value, minimum, maximum);
}
