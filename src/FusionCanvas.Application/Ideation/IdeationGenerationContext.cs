using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationGenerationContext(
    IdeationCreativeContext Store,
    IdeationCreativeContext Niche,
    IdeationCreativeContext? Group,
    string? Guidance,
    IdeationMode Mode,
    string? SnowcloneTemplate,
    string? SnowcloneGuidance,
    IReadOnlyList<string> SnowclonePlaceholderTokens,
    IReadOnlyList<string> ActiveIdeas,
    IReadOnlyList<IdeationRejectedContext> RejectedIdeas);
