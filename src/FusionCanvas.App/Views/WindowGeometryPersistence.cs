using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FusionCanvas.Application.Settings;
using System.Runtime.InteropServices;

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
        var persistedOnClosing = false;
        var captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        void CaptureNormalGeometry()
        {
            var position = TryGetNativePosition(window, out var nativePosition)
                ? nativePosition
                : window.Position;
            if (MainWindowLayoutNormalizer.TryCaptureGeometry(
                window.WindowState,
                position,
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
        window.Closing += (_, args) =>
        {
            if (!args.Cancel)
            {
                CaptureNormalGeometry();
                if (lastNormalGeometry is not null)
                {
                    store.UpdateWindowGeometry(windowKey, lastNormalGeometry);
                    persistedOnClosing = true;
                }
            }
        };
        window.Closed += (_, _) =>
        {
            captureTimer.Stop();
            CaptureNormalGeometry();
            if (!persistedOnClosing && lastNormalGeometry is not null)
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

    private static bool TryGetNativePosition(Window window, out PixelPoint position)
    {
        position = default;
        if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle()?.Handle is not { } handle)
        {
            return false;
        }

        if (!GetWindowRect(handle, out var rect))
        {
            return false;
        }

        position = new PixelPoint(rect.Left, rect.Top);
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
