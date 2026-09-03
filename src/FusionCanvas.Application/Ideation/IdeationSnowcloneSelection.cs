using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationSnowcloneSelection(
    Guid Id,
    string Phrase,
    string Guidance,
    IReadOnlyList<string> PlaceholderTokens);
