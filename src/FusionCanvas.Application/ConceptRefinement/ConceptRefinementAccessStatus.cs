using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.ConceptRefinement;

public sealed record ConceptRefinementAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static ConceptRefinementAccessAvailability Available { get; } = new(true, null);

    public static ConceptRefinementAccessAvailability Unavailable(string reason) => new(false, reason);
}

public interface IConceptRefinementAccessStatus
{
    event EventHandler? AvailabilityChanged
    {
        add { }
        remove { }
    }

    ConceptRefinementAccessAvailability GetAvailability();

    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class ConfiguredConceptRefinementAccessStatus(IAiTextGenerationService ai)
    : IConceptRefinementAccessStatus
{
    private readonly IAiTextGenerationService _ai =
        ai ?? throw new ArgumentNullException(nameof(ai));
    private ConceptRefinementAccessAvailability _current =
        ConceptRefinementAccessAvailability.Unavailable("Checking AI configuration…");

    public event EventHandler? AvailabilityChanged;

    public ConceptRefinementAccessAvailability GetAvailability() => _current;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var availability = await _ai
            .GetAvailabilityAsync(AiRequestPurpose.Concept, cancellationToken)
            .ConfigureAwait(false);
        var next = availability.IsReady
            ? ConceptRefinementAccessAvailability.Available
            : ConceptRefinementAccessAvailability.Unavailable(availability.Message);
        if (next != _current)
        {
            _current = next;
            AvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}