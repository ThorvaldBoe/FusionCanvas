namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogImportService
{
    Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default) =>
        LoadSelectedAsync(scope, productIds.Select(value => int.TryParse(value, out var id) ? id : -1).ToArray(), cancellationToken);
}
