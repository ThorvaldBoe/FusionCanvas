namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogClient
{
    async Task<PrintifyCatalogResult> LoadShopBlueprintsAsync(string key, int shopId, CancellationToken cancellationToken = default)
    {
        var result = await LoadShopProductsAsync(key, shopId, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || result.Products is null) return result;
        var blueprints = result.Products
            .GroupBy(product => product.BlueprintId)
            .Select(group => new PrintifyShopBlueprintSummary(
                group.Key,
                group.Select(product => product.BlueprintName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Blueprint {group.Key}",
                group.Count(),
                group.Select(product => product.ProviderId).Distinct().Select(id => new PrintifyShopProviderSummary(id, $"Provider {id}")).ToArray(),
                group.Select(product => product.Title).Where(title => !string.IsNullOrWhiteSpace(title)).Distinct(StringComparer.Ordinal).ToArray()))
            .OrderBy(blueprint => blueprint.BlueprintName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return result with { ShopBlueprints = blueprints };
    }

    async Task<PrintifyCatalogResult> LoadSelectedBlueprintsAsync(string key, int shopId, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default)
    {
        var result = await LoadSelectedAsync(key, blueprintIds, cancellationToken).ConfigureAwait(false);
        return result with { SelectedProducts = result.SelectedCatalog };
    }

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
