using Avalonia;
using Avalonia.Controls;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Views;

internal static class WindowGeometryPersistence
{
    public static void Attach(
        Window window,
        IWindowGeometryStore store,
        string windowKey,
        double minimumWindowWidth,
        double minimumWindowHeight)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(windowKey);

        window.Opened += (_, _) => Restore(window, store, windowKey, minimumWindowWidth, minimumWindowHeight);
        window.Closing += (_, _) => Capture(window, store, windowKey, minimumWindowWidth, minimumWindowHeight);
    }

    private static void Restore(
        Window window,
        IWindowGeometryStore store,
        string windowKey,
        double minimumWindowWidth,
        double minimumWindowHeight)
    {
        if (!store.WindowGeometry.TryGetValue(windowKey, out var saved))
        {
            return;
        }

        var screens = GetScreens(window);
        if (!MainWindowLayoutNormalizer.TryNormalizeGeometry(
                saved, screens, minimumWindowWidth, minimumWindowHeight, out var normalized))
        {
            return;
        }

        window.Width = normalized.Width;
        window.Height = normalized.Height;
        window.Position = new PixelPoint(normalized.PositionX, normalized.PositionY);
    }

    private static void Capture(
        Window window,
        IWindowGeometryStore store,
        string windowKey,
        double minimumWindowWidth,
        double minimumWindowHeight)
    {
        if (!MainWindowLayoutNormalizer.TryCaptureGeometry(
                window.WindowState,
                window.Position,
                window.Width,
                window.Height,
                minimumWindowWidth,
                minimumWindowHeight,
                out var geometry))
        {
            return;
        }

        store.UpdateWindowGeometry(windowKey, geometry);
    }

    private static ScreenLayoutInfo[] GetScreens(Window window) =>
        window.Screens.All
            .Where(screen => screen.WorkingArea.Width > 0 && screen.WorkingArea.Height > 0)
            .Select(screen => new ScreenLayoutInfo(screen.WorkingArea, screen.Scaling, screen.IsPrimary))
            .ToArray();
}
