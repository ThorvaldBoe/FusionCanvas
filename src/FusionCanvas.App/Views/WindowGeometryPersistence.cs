using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
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

        WindowGeometrySettings? lastNormalGeometry = null;
        var captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        void CaptureNormalGeometry()
        {
            if (MainWindowLayoutNormalizer.TryCaptureGeometry(
                    window.WindowState,
                    window.Position,
                    window.Width,
                    window.Height,
                    minimumWindowWidth,
                    minimumWindowHeight,
                    out var geometry))
            {
                lastNormalGeometry = geometry;
            }
        }

        captureTimer.Tick += (_, _) => CaptureNormalGeometry();

        window.Opened += (_, _) =>
        {
            Restore(window, store, windowKey, minimumWindowWidth, minimumWindowHeight);
            CaptureNormalGeometry();
            captureTimer.Start();
        };
        window.SizeChanged += (_, _) => CaptureNormalGeometry();
        window.PositionChanged += (_, _) => CaptureNormalGeometry();
        window.Closed += (_, _) =>
        {
            captureTimer.Stop();
            CaptureNormalGeometry();
            if (lastNormalGeometry is not null)
            {
                store.UpdateWindowGeometry(windowKey, lastNormalGeometry);
            }
        };
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

    private static ScreenLayoutInfo[] GetScreens(Window window) =>
        window.Screens.All
            .Where(screen => screen.WorkingArea.Width > 0 && screen.WorkingArea.Height > 0)
            .Select(screen => new ScreenLayoutInfo(screen.WorkingArea, screen.Scaling, screen.IsPrimary))
            .ToArray();
}
