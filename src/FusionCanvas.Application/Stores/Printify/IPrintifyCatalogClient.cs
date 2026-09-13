namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogClient
{
    async Task<PrintifyCatalogResult> LoadShopProductsAsync(string key, int shopId, CancellationToken cancellationToken = default)
    {
        var result = await LoadBlueprintsAsync(key, cancellationToken).ConfigureAwait(false);
        return result with
        {
            Products = result.Blueprints?.Select(value => new PrintifyShopProductSummary(value.Id.ToString(), value.Title, value.Description, value.Id, 1)).ToArray()
        };
    }

    async Task<PrintifyCatalogResult> LoadSelectedProductsAsync(string key, int shopId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds.Select(id => int.TryParse(id, out var value) ? value : -1).ToArray();
        var result = await LoadSelectedAsync(key, ids, cancellationToken).ConfigureAwait(false);
        return result with { SelectedProducts = result.SelectedCatalog };
    }

    Task<PrintifyCatalogResult> LoadBlueprintsAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<PrintifyCatalogResult> LoadSelectedAsync(
        string key,
        IReadOnlyCollection<int> blueprintIds,
        CancellationToken cancellationToken = default);
}
