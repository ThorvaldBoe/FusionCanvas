namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyConfigurationResult(PrintifyConfigurationKind Kind, string Message, IReadOnlyList<PrintifyShopOption>? Shops = null)
{
    public bool Succeeded => Kind is PrintifyConfigurationKind.Saved or PrintifyConfigurationKind.Verified;
    public static PrintifyConfigurationResult Unavailable { get; } =
        new(PrintifyConfigurationKind.Unavailable, "Native credential storage is unavailable, locked, or access was denied. Retry after unlocking it.");
}
