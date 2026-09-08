namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogImportService
{
    Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default);
}
