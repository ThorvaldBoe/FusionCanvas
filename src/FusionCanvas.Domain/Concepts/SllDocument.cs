namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// The full-minimal Sketch Layout Language (SLL) artifact produced from a Design Triangle,
/// following the canonical framework's minimal command semantics: assumptions, communication
/// intent, the normalized Design Triangle, one ASCII sketch, execution notes, and validation
/// with the largest risk.
/// </summary>
public sealed record SllDocument(
    IReadOnlyList<string> Assumptions,
    SllCommunication Communication,
    SllTriangle Triangle,
    string AsciiSketch,
    SllNotes Notes,
    SllValidation Validation)
{
    /// <summary>
    /// Validates the hard SLL invariants: the ASCII sketch is non-empty and the triangle's
    /// phrase preserves the supplied phrase unless an explicit revision is recorded.
    /// </summary>
    public bool Validate(string suppliedPhrase) =>
        !string.IsNullOrWhiteSpace(AsciiSketch)
        && Triangle.IsPhrasePreserved(suppliedPhrase);
}

public sealed record SllCommunication(
    string? WearerSignal,
    string? ViewerInference,
    string? Emotion,
    string? SharedContext)
{
    public static SllCommunication Empty { get; } = new(null, null, null, null);
}

public sealed record SllTriangle(
    string? Idea,
    string? Phrase,
    string? Graphic,
    string? Relationship,
    string? RevisedPhrase)
{
    public bool IsPhrasePreserved(string suppliedPhrase)
    {
        if (string.Equals(Phrase, suppliedPhrase, StringComparison.Ordinal))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(RevisedPhrase);
    }
}

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

public sealed record SllValidation(
    string? ReadingOrder,
    string? Thumbnail,
    string? Signal,
    string? LargestRisk)
{
    public static SllValidation Empty { get; } = new(null, null, null, null);
}
