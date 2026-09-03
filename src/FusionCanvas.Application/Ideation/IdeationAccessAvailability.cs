using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static IdeationAccessAvailability Available { get; } = new(true, null);

    public static IdeationAccessAvailability Unavailable(string reason) => new(false, reason);
}
