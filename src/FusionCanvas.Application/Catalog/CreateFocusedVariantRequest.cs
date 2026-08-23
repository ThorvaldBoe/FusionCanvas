namespace FusionCanvas.Application.Catalog;

public sealed record CreateFocusedVariantRequest(OfferingContext Context, string Name, IReadOnlyList<Guid> OptionValueIds);
