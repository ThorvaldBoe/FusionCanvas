using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Workspace;

internal sealed class UiTestPrintifyCatalogClient : IPrintifyCatalogClient
{
    public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "No fixture catalog."));
    public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "No fixture catalog."));

    public Task<PrintifyCatalogResult> LoadSelectedProductsAsync(string key, int shopId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "Fixture catalog loaded.", SelectedProducts:
        [new PrintifyCatalogBlueprint(new(68, "Mocked Tee", "Imported from the UI test fixture.", "Test", "Tee"),
            [new PrintifyCatalogProvider(9, "Fixture Provider", [new("Color", "color", [new(1, "Black")])],
                [new(11, "Black / M", true, true, [1], [new("front", "dtg", 100, 200)])])]) { ProductId = "ui-test-product" }]));
}
