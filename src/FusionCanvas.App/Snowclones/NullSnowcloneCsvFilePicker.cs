namespace FusionCanvas.App.Snowclones;

public sealed class NullSnowcloneCsvFilePicker : ISnowcloneCsvFilePicker
{
    public Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream?>(null);
    }

    public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream?>(null);
    }
}
