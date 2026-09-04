using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.SllGeneration;

public sealed record SllAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static SllAccessAvailability Available { get; } = new(true, null);

    public static SllAccessAvailability Unavailable(string reason) => new(false, reason);
}
