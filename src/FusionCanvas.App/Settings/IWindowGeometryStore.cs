using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Settings;

public interface IWindowGeometryStore
{
    IReadOnlyDictionary<string, WindowGeometrySettings> WindowGeometry { get; }

    void UpdateWindowGeometry(string windowKey, WindowGeometrySettings? geometry);
}
