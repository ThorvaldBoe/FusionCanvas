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
