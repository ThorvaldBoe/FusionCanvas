using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateFocusedDesignAreaRequest(OfferingContext Context, string Name, string Placement, string DecorationMethod, int MaximumWidthPixels, int MaximumHeightPixels, IReadOnlyList<Guid> VariantIds, bool UseAllActiveVariants = true, string? Description = null, string? ProviderReference = null, DesignAreaArtworkGuidance? ArtworkGuidance = null);
