using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record ProductVariantSummary
(
    Guid Id,
    Guid OfferingId,
    IReadOnlyList<VariantOption> Options)
{
    public string DisplayName => string.Join(", ", Options.Select(option => $"{option.Name}: {option.Value}"));
}
