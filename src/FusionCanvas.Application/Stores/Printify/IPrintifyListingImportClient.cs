namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyListingImportClient
{
    Task<IReadOnlyList<PrintifyListingProductSummary>> GetShopProductsAsync(string apiKey, int shopId, CancellationToken cancellationToken = default);
    Task<PrintifyListingProductDetail?> GetProductAsync(string apiKey, int shopId, string productId, CancellationToken cancellationToken = default);
    Task<PrintifyArtworkDownload> DownloadArtworkAsync(string sourceUrl, CancellationToken cancellationToken = default);
}
