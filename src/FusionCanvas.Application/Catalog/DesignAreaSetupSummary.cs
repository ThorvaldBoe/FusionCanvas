using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.Catalog;

public sealed record DesignAreaSetupSummary(Guid Id, string Name, string Placement, int MaximumWidthPixels, int MaximumHeightPixels, DesignAreaPhysicalSize? SecondaryPhysicalSize, DesignAreaArtworkGuidance? ArtworkGuidance, bool AppliesToAllActiveVariants, int CompatibleVariantCount, string? ProviderReference);
