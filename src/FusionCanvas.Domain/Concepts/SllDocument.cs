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
    /// Indicates whether the document contains every required top-level SLL section.
    /// Optional values inside those sections may be empty, but the sections themselves
    /// must be present for a persisted SLL to be displayable.
    /// </summary>
    public bool IsStructurallyComplete =>
        Assumptions is not null
        && Communication is not null
        && Triangle is not null
        && !string.IsNullOrWhiteSpace(AsciiSketch)
        && Notes is not null
        && Validation is not null;

    /// <summary>
    /// Validates the hard SLL invariants: the ASCII sketch is non-empty and the triangle's
    /// phrase preserves the supplied phrase unless an explicit revision is recorded.
    /// </summary>
    public bool Validate(string suppliedPhrase) =>
        IsStructurallyComplete
        && Triangle.IsPhrasePreserved(suppliedPhrase);
}
