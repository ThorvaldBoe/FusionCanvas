namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogImportService
{
    Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
    Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken, IProgress<PrintifyCatalogImportProgress>? progress) =>
        LoadBlueprintsAsync(scope, cancellationToken);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken, IProgress<PrintifyCatalogImportProgress>? progress) =>
        LoadSelectedAsync(scope, blueprintIds, cancellationToken);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default) =>
        LoadSelectedAsync(scope, productIds.Select(value => int.TryParse(value, out var id) ? id : -1).ToArray(), cancellationToken);
    Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken, IProgress<PrintifyCatalogImportProgress>? progress) =>
        LoadSelectedAsync(scope, productIds.Select(value => int.TryParse(value, out var id) ? id : -1).ToArray(), cancellationToken, progress);
}
