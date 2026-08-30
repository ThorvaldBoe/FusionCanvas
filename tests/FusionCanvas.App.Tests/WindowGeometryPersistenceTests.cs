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
        window.Position = new Avalonia.PixelPoint(120, 80);
        window.UpdateLayout();
        window.UpdateLayout();
        window.Close();
        window.UpdateLayout();

        var captured = Assert.Single(store.Updates, u => u.Key == WindowLayoutKeys.Settings);
        Assert.NotNull(captured.Geometry);
        Assert.Equal(700, captured.Geometry!.Width);
        Assert.Equal(520, captured.Geometry!.Height);
        Assert.Equal(120, captured.Geometry.PositionX);
        Assert.Equal(80, captured.Geometry.PositionY);
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
        Assert.Equal(new Avalonia.PixelPoint(10, 10), window.Position);
    }

    [AvaloniaFact]
    public void Attach_UsesIndependentGeometryForEachWindowKey()
    {
        var store = new RecordingGeometryStore();
        store.Stored[WindowLayoutKeys.Settings] = new WindowGeometrySettings(10, 10, 640, 480);
        store.Stored[WindowLayoutKeys.StoreEditor] = new WindowGeometrySettings(120, 80, 700, 520);

        var settings = new Window();
        var storeEditor = new Window();
        WindowGeometryPersistence.Attach(settings, store, WindowLayoutKeys.Settings, 200, 150);
        WindowGeometryPersistence.Attach(storeEditor, store, WindowLayoutKeys.StoreEditor, 200, 150);

        settings.Show();
        storeEditor.Show();
        settings.UpdateLayout();
        storeEditor.UpdateLayout();

        Assert.Equal(new Avalonia.PixelPoint(10, 10), settings.Position);
        Assert.Equal(new Avalonia.PixelPoint(120, 80), storeEditor.Position);
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

    [AvaloniaFact]
    public void Attach_ClosingWhileMaximizedPersistsLatestNormalGeometry()
    {
        var store = new RecordingGeometryStore();
        var window = new Window();
        WindowGeometryPersistence.Attach(window, store, WindowLayoutKeys.Settings, 200, 150);
        window.Show();
        window.Width = 720;
        window.Height = 540;
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        window.WindowState = WindowState.Maximized;
        window.Close();

        var captured = Assert.Single(store.Updates, u => u.Key == WindowLayoutKeys.Settings);
        Assert.Equal(720, captured.Geometry!.Width);
        Assert.Equal(540, captured.Geometry.Height);
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
