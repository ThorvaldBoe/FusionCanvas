using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record FulfillmentOfferingSummary
(
    Guid Id,
    Guid ProductId,
    string Name,
    string? Description,
    FulfillmentKind Kind,
    string? ProviderName,
    string? ExternalOfferingId,
    IReadOnlyList<ProductVariantSummary> Variants,
    IReadOnlyList<DesignAreaSummary> DesignAreas)
{
    public string FulfillmentContextLabel => Kind == FulfillmentKind.FixedProvider
        ? $"Print Provider: {ProviderName ?? "Not configured"}"
        : "Provider Network: Printify Choice (fulfillment partner can vary)";

    public string SetupSummary =>
        $"{Variants.Count} Variant{(Variants.Count == 1 ? string.Empty : "s")} · {DesignAreas.Count} Design Area{(DesignAreas.Count == 1 ? string.Empty : "s")}";
}
