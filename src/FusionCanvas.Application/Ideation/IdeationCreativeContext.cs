using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationCreativeContext(
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string> Metadata);
