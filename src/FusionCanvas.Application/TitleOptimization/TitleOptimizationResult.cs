using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.TitleOptimization;

public sealed record TitleOptimizationResult(
    bool Succeeded,
    string? Title,
    string? Error,
    AiTextFailureKind? FailureKind = null)
{
    public static TitleOptimizationResult Success(string title) => new(true, title, null, null);

    public static TitleOptimizationResult Failure(string error, AiTextFailureKind? kind = null) =>
        new(false, null, error, kind);
}
