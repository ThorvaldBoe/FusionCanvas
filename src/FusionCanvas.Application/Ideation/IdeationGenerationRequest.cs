using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationGenerationRequest(
    IdeationScope Scope,
    IdeationMode Mode,
    string? Guidance,
    int Count,
    IReadOnlyCollection<string>? ExistingCandidates = null);
