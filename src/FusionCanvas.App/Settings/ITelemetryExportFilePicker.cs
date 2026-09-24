namespace FusionCanvas.App.Settings;

public interface ITelemetryExportFilePicker
{
    Task<string?> PickPathAsync(CancellationToken cancellationToken = default);
}
