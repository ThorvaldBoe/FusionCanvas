namespace FusionCanvas.App.Items;

public interface IItemCsvFilePicker
{
    Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default);
}
