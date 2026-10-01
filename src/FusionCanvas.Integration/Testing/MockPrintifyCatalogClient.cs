using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.Integration.Testing;

public sealed class MockPrintifyCatalogClient : IPrintifyCatalogClient
{
    public MockPrintifyCatalogClient(
        IReadOnlyList<PrintifyCatalogBlueprint>? blueprints = null,
        IReadOnlyList<PrintifyShopProductSummary>? products = null)
    {
        Blueprints = blueprints ?? CreateBlueprints();
        Products = products ??
        [
            new("mock-product-1", "Synthetic Shop Product", "Synthetic product description.", 1001, 2001, "Synthetic Tee")
        ];
    }

    public IReadOnlyList<PrintifyCatalogBlueprint> Blueprints { get; }

    public IReadOnlyList<PrintifyShopProductSummary> Products { get; }

    public PrintifyCatalogResult? BlueprintsResultOverride { get; set; }

    public PrintifyCatalogResult? SelectedResultOverride { get; set; }

    public PrintifyCatalogResult? ShopProductsResultOverride { get; set; }

    public PrintifyCatalogResult? SelectedProductsResultOverride { get; set; }

    public List<CatalogRequestObservation> Requests { get; } = [];

    public Task<PrintifyCatalogResult> LoadBlueprintsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(new(nameof(LoadBlueprintsAsync), null, [], []));
        return Task.FromResult(BlueprintsResultOverride ?? CreateBlueprintsResult());
    }

    public Task<PrintifyCatalogResult> LoadSelectedAsync(
        string key,
        IReadOnlyCollection<int> blueprintIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blueprintIds);
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(new(nameof(LoadSelectedAsync), null, blueprintIds.ToArray(), []));
        if (SelectedResultOverride is not null) return Task.FromResult(SelectedResultOverride);
        if (!PrintifyToken.IsValid(key)) return Task.FromResult(InvalidKey());
        if (blueprintIds.Count == 0) return Task.FromResult(InvalidRequest("Select at least one synthetic blueprint."));

        var selected = Blueprints.Where(value => blueprintIds.Contains(value.Summary.Id)).ToArray();
        return Task.FromResult(selected.Length == blueprintIds.Distinct().Count()
            ? new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "Synthetic catalog data loaded.", Blueprints.Select(value => value.Summary).ToArray(), selected)
            : InvalidRequest("One or more synthetic blueprints is unavailable."));
    }

    public Task<PrintifyCatalogResult> LoadShopProductsAsync(
        string key,
        int shopId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(new(nameof(LoadShopProductsAsync), shopId, [], []));
        if (ShopProductsResultOverride is not null) return Task.FromResult(ShopProductsResultOverride);
        if (!PrintifyToken.IsValid(key)) return Task.FromResult(InvalidKey());
        if (shopId <= 0) return Task.FromResult(InvalidRequest("Select a valid synthetic shop."));
        return Task.FromResult(Products.Count == 0
            ? new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "The synthetic shop has no products.", Products: Products)
            : new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "Synthetic shop products loaded.", Products: Products));
    }

    public Task<PrintifyCatalogResult> LoadSelectedProductsAsync(
        string key,
        int shopId,
        IReadOnlyCollection<string> productIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(new(nameof(LoadSelectedProductsAsync), shopId, [], productIds.ToArray()));
        if (SelectedProductsResultOverride is not null) return Task.FromResult(SelectedProductsResultOverride);
        if (!PrintifyToken.IsValid(key)) return Task.FromResult(InvalidKey());
        if (shopId <= 0 || productIds.Count == 0 || productIds.Any(string.IsNullOrWhiteSpace))
            return Task.FromResult(InvalidRequest("Select at least one valid synthetic product."));

        var selectedIds = productIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var selectedProducts = Products.Where(value => selectedIds.Contains(value.ProductId)).ToArray();
        if (selectedProducts.Length != selectedIds.Count)
            return Task.FromResult(InvalidRequest("One or more synthetic products is unavailable."));

        var selectedBlueprints = selectedProducts
            .Select(product => Blueprints.SingleOrDefault(blueprint => blueprint.Summary.Id == product.BlueprintId))
            .Where(value => value is not null)
            .Cast<PrintifyCatalogBlueprint>()
            .ToArray();
        return Task.FromResult(new PrintifyCatalogResult(
            PrintifyCatalogResultKind.Succeeded,
            "Synthetic selected products loaded.",
            Blueprints.Select(value => value.Summary).ToArray(),
            SelectedCatalog: selectedBlueprints,
            Products,
            SelectedProducts: selectedBlueprints));
    }

    private PrintifyCatalogResult CreateBlueprintsResult() => Blueprints.Count == 0
        ? new(PrintifyCatalogResultKind.Empty, "The synthetic catalog is empty.", Blueprints: [])
        : new(PrintifyCatalogResultKind.Succeeded, "Synthetic blueprints loaded.", Blueprints.Select(value => value.Summary).ToArray());

    private static PrintifyCatalogBlueprint[] CreateBlueprints() =>
    [
        new(
            new(1001, "Synthetic Tee", "Synthetic catalog blueprint.", "Synthetic Brand", "ST-1001"),
            [new(
                2001,
                "Synthetic Provider",
                [new("Color", "color", [new(3001, "Synthetic Black"), new(3002, "Synthetic White")])],
                [new(4001, "Synthetic Black", true, true, [3001], [new("front", "dtg", 1000, 1200)])])])
    ];

    private static PrintifyCatalogResult InvalidKey() =>
        new(PrintifyCatalogResultKind.InvalidKey, "The synthetic Printify key is invalid.");

    private static PrintifyCatalogResult InvalidRequest(string message) =>
        new(PrintifyCatalogResultKind.InvalidRequest, message);

    public sealed record CatalogRequestObservation(
        string Operation,
        int? ShopId,
        IReadOnlyList<int> BlueprintIds,
        IReadOnlyList<string> ProductIds);
}
