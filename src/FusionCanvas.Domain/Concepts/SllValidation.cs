namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// The full-minimal Sketch Layout Language (SLL) artifact produced from a Design Triangle,
/// following the canonical framework's minimal command semantics: assumptions, communication
/// intent, the normalized Design Triangle, one ASCII sketch, execution notes, and validation
/// with the largest risk.
/// </summary>
public sealed record SllValidation(
    string? ReadingOrder,
    string? Thumbnail,
    string? Signal,
    string? LargestRisk)
{
    public static SllValidation Empty { get; } = new(null, null, null, null);
}
