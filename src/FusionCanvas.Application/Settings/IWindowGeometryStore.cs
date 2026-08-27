namespace FusionCanvas.Application.Settings;

public interface IWindowGeometryStore
{
    IReadOnlyDictionary<string, WindowGeometrySettings> WindowGeometry { get; }

    void UpdateWindowGeometry(string windowKey, WindowGeometrySettings? geometry);
}
