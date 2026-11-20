namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyShopBlueprintSummary(
    int BlueprintId,
    string BlueprintName,
    int ShopProductCount,
    IReadOnlyList<PrintifyShopProviderSummary> Providers,
    IReadOnlyList<string> ShopProductTitles);

public sealed record PrintifyShopProviderSummary(int ProviderId, string Name);
