namespace FusionCanvas.Application.AI;

public sealed record ArtworkGenerationReadiness(bool IsReady, IReadOnlyList<string> Blockers);
