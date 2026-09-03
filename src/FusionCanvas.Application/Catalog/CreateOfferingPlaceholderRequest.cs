using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateOfferingPlaceholderRequest(Guid OfferingId, string Name, string Position, string DecorationMethod, int Width, int Height, IReadOnlyList<Guid> VariantIds, string? Description = null, bool UseAllActiveVariants = false, string? ProviderReference = null, DesignAreaArtworkGuidance? ArtworkGuidance = null);
