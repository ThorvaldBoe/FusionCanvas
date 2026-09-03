namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// The full-minimal Sketch Layout Language (SLL) artifact produced from a Design Triangle,
/// following the canonical framework's minimal command semantics: assumptions, communication
/// intent, the normalized Design Triangle, one ASCII sketch, execution notes, and validation
/// with the largest risk.
/// </summary>
public sealed record SllCommunication(
    string? WearerSignal,
    string? ViewerInference,
    string? Emotion,
    string? SharedContext)
{
    public static SllCommunication Empty { get; } = new(null, null, null, null);
}
