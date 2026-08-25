using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FusionCanvas.Application.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests;

public class WindowGeometryPersistenceTests
{
    [AvaloniaFact]
    public void Attach_ClosingCapturesNormalStateGeometry()
    {
        var store = new RecordingGeometryStore();
        var window = new Window();
        WindowGeometryPersistence.Attach(window, store, WindowLayoutKeys.Settings, 200, 150);
        window.Show();
        window.Width = 700;
        window.Height = 520;
        window.UpdateLayout();
        window.UpdateLayout();
        window.Close();
        window.UpdateLayout();

        var captured = Assert.Single(store.Updates, u => u.Key == WindowLayoutKeys.Settings);
        Assert.NotNull(captured.Geometry);
        Assert.Equal(700, captured.Geometry!.Width);
        Assert.Equal(520, captured.Geometry!.Height);
        Assert.Equal(new WindowGeometrySettings(captured.Geometry!.PositionX, captured.Geometry!.PositionY, 700, 520),
            store.WindowGeometry[WindowLayoutKeys.Settings]);
    }

    [AvaloniaFact]
    public void Attach_OpenedRestoresSavedGeometryWhenScreenAllows()
    {
        var store = new RecordingGeometryStore();
        store.Stored[WindowLayoutKeys.Settings] = new WindowGeometrySettings(10, 10, 640, 480);
        var window = new Window();
        WindowGeometryPersistence.Attach(window, store, WindowLayoutKeys.Settings, 200, 150);
        window.Show();
        window.UpdateLayout();
        window.UpdateLayout();

        Assert.Equal(640, window.Width);
        Assert.Equal(480, window.Height);
    }

    [AvaloniaFact]
    public void Attach_OpenedWithoutSavedGeometryLeavesDefaults()
    {
        var store = new RecordingGeometryStore();
        var window = new Window();
        var defaultWidth = window.Width;
        var defaultHeight = window.Height;
        WindowGeometryPersistence.Attach(window, store, WindowLayoutKeys.Settings, 200, 150);
        window.Show();
        window.UpdateLayout();
        window.UpdateLayout();

        Assert.Equal(defaultWidth, window.Width);
        Assert.Equal(defaultHeight, window.Height);
    }

    private sealed class RecordingGeometryStore : IWindowGeometryStore
    {
        public Dictionary<string, WindowGeometrySettings> Stored { get; } = new();

        public List<(string Key, WindowGeometrySettings? Geometry)> Updates { get; } = new();

        public IReadOnlyDictionary<string, WindowGeometrySettings> WindowGeometry => Stored;

        public void UpdateWindowGeometry(string windowKey, WindowGeometrySettings? geometry)
        {
            Updates.Add((windowKey, geometry));
            if (geometry is null)
            {
                Stored.Remove(windowKey);
            }
            else
            {
                Stored[windowKey] = geometry;
            }
        }
    }
}
