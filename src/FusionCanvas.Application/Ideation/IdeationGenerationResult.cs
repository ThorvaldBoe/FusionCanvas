using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationGenerationResult(
    bool Succeeded,
    bool Cancelled,
    IReadOnlyList<IdeationCandidate> Candidates,
    int Requested,
    int Completed,
    int Failed,
    string? Error,
    Exception? TechnicalCause = null)
{
    public static IdeationGenerationResult Failure(
        string error,
        int requested = 0,
        Exception? technicalCause = null) =>
        new(false, false, [], requested, 0, requested, error, technicalCause);
}
