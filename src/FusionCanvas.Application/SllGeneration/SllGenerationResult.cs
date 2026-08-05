using FusionCanvas.Application.AI;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.Application.SllGeneration;

public sealed record SllGenerationResult(
    bool Succeeded,
    SllDocument? Document,
    AiTextFailureKind? FailureKind,
    string? Error)
{
    public static SllGenerationResult Success(SllDocument document) =>
        new(true, document, null, null);

    public static SllGenerationResult Failure(AiTextFailureKind kind, string error) =>
        new(false, null, kind, error);
}
