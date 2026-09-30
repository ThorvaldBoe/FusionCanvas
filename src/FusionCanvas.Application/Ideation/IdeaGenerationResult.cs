using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeaGenerationResult(
    bool Succeeded,
    string? Text,
    AiTextFailureKind? FailureKind,
    string? Error,
    Exception? TechnicalCause = null)
{
    public static IdeaGenerationResult Success(string text) => new(true, text, null, null);

    public static IdeaGenerationResult Failure(
        AiTextFailureKind kind,
        string error,
        Exception? technicalCause = null) =>
        new(false, null, kind, error, technicalCause);
}
