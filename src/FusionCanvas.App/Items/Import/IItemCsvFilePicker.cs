namespace FusionCanvas.App.Items.Import;

public interface IItemCsvFilePicker
{
    Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default);

    Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default);
}
