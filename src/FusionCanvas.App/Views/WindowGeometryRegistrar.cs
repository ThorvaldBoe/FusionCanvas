using Avalonia.Controls;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Views;

/// <summary>
/// Application-wide entry point for registering non-transient window geometry.
/// </summary>
internal static class WindowGeometryRegistrar
{
    private static readonly Dictionary<string, Window> ActiveRegistrations = new(StringComparer.Ordinal);

    public static void Register(
        Window window,
        IWindowGeometryStore store,
        string windowKey,
        double minimumWindowWidth,
        double minimumWindowHeight)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(windowKey))
        {
            throw new ArgumentException("A stable window key is required.", nameof(windowKey));
        }

        if (ActiveRegistrations.ContainsKey(windowKey))
        {
            throw new InvalidOperationException($"Window key '{windowKey}' is already registered.");
        }

        ActiveRegistrations[windowKey] = window;
        window.Closed += (_, _) => ActiveRegistrations.Remove(windowKey);

        WindowGeometryPersistence.Attach(
            window,
            store,
            windowKey,
            minimumWindowWidth,
            minimumWindowHeight);
    }
}
