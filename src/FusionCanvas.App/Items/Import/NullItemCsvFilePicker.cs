namespace FusionCanvas.App.Items.Import;

public sealed class NullItemCsvFilePicker : IItemCsvFilePicker
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