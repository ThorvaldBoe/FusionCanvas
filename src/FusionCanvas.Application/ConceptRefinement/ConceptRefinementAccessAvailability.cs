using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.ConceptRefinement;

public sealed record ConceptRefinementAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static ConceptRefinementAccessAvailability Available { get; } = new(true, null);

    public static ConceptRefinementAccessAvailability Unavailable(string reason) => new(false, reason);
}
