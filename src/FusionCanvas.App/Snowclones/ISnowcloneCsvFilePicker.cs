namespace FusionCanvas.App.Snowclones;

public interface ISnowcloneCsvFilePicker
{
    Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default);

    Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default);
}
