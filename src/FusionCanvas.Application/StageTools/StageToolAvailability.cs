using FusionCanvas.Application.ToolContexts;

namespace FusionCanvas.Application.StageTools;

public sealed record StageToolAvailability(
    StageToolDescriptor Tool,
    StageToolAvailabilityKind Kind,
    string? Reason,
    ToolContextResolution? ToolContext)
{
    public bool IsAvailable => Kind == StageToolAvailabilityKind.Available;
}
