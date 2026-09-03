namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// The full-minimal Sketch Layout Language (SLL) artifact produced from a Design Triangle,
/// following the canonical framework's minimal command semantics: assumptions, communication
/// intent, the normalized Design Triangle, one ASCII sketch, execution notes, and validation
/// with the largest risk.
/// </summary>
public sealed record SllNotes(
    string? Composition,
    string? Typography,
    string? GraphicStyle,
    string? Colors,
    string? TextureEffects,
    string? PlacementScale,
    string? Production)
{
    public static SllNotes Empty { get; } = new(null, null, null, null, null, null, null);
}
