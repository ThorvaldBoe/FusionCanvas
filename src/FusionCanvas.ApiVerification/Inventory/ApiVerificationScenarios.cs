namespace FusionCanvas.ApiVerification.Inventory;

internal static class ApiVerificationScenarios
{
    public static IReadOnlySet<string> OpenRouter { get; } = new HashSet<string>(
        ["openrouter-smoke", "openrouter-catalog", "openrouter-text-generation", "openrouter-image-generation", "openrouter-qualification"],
        StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> Printify { get; } = new HashSet<string>(
        ["printify-smoke", "printify-catalog", "printify-shop-products", "printify-product-detail", "printify-artwork-import", "printify-product-lifecycle"],
        StringComparer.OrdinalIgnoreCase);
}
