namespace FusionCanvas.App.Settings;

public interface ITelemetryExportFilePicker
{
    Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default);
}
