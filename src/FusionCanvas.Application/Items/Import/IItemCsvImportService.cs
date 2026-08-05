namespace FusionCanvas.Application.Items.Import;

public interface IItemCsvImportService
{
    Task<ItemCsvImportResult> ImportAsync(
        ItemCsvImportRequest request,
        CancellationToken cancellationToken = default);
}
