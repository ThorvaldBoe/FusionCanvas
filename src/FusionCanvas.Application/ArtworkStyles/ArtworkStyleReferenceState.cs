using FusionCanvas.Domain.ArtworkStyles;

namespace FusionCanvas.Application.ArtworkStyles;

public sealed record ArtworkStyleReferenceState(ArtworkStyleReference Reference, bool IsAvailable);
