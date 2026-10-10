using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Workspace;

internal sealed class UiTestPrintifyListingImportClient : IPrintifyListingImportClient
{
    public Task<IReadOnlyList<PrintifyListingProductSummary>> GetShopProductsAsync(string apiKey, int shopId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PrintifyListingProductSummary>>([new("ui-test-product", "Mocked Printify art", "Imported from the UI test fixture.", true, 68, 9)]);

    public Task<PrintifyListingProductDetail?> GetProductAsync(string apiKey, int shopId, string productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PrintifyListingProductDetail?>(productId != "ui-test-product" ? null : new(
            "ui-test-product", "Mocked Printify art", "Imported from the UI test fixture.", true, 68, 9, null,
            [new("Color", "color", [new(1, "Black")])], [new(11, "Black / M", 2500, true, true, [1])],
            [new("front", [11], [new("ui-test-image", "https://images.printify.com/ui-test.png", "Test artwork", "image/png", "front", [11])], false)]));

    public Task<PrintifyArtworkDownload> DownloadArtworkAsync(string sourceUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PrintifyArtworkDownload([137, 80, 78, 71, 13, 10, 26, 10], "image/png", ".png"));
}
