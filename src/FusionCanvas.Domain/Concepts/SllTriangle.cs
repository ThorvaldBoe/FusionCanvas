namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// The full-minimal Sketch Layout Language (SLL) artifact produced from a Design Triangle,
/// following the canonical framework's minimal command semantics: assumptions, communication
/// intent, the normalized Design Triangle, one ASCII sketch, execution notes, and validation
/// with the largest risk.
/// </summary>
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
