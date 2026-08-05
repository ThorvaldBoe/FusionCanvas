namespace FusionCanvas.App.Items;

public sealed class NullItemCsvFilePicker : IItemCsvFilePicker
{
    public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream?>(null);
    }
}
