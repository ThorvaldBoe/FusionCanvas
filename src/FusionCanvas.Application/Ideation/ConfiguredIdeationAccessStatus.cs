using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed class ConfiguredIdeationAccessStatus(IAiTextGenerationService ai) : IIdeationAccessStatus
{
    private readonly IAiTextGenerationService _ai =
        ai ?? throw new ArgumentNullException(nameof(ai));
    private IdeationAccessAvailability _current =
        IdeationAccessAvailability.Unavailable("Checking AI configuration…");

    public event EventHandler? AvailabilityChanged;

    public IdeationAccessAvailability GetAvailability() => _current;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var availability = await _ai
            .GetAvailabilityAsync(AiRequestPurpose.Ideation, cancellationToken)
            .ConfigureAwait(false);
        var next = availability.IsReady
            ? IdeationAccessAvailability.Available
            : IdeationAccessAvailability.Unavailable(availability.Message);
        if (next != _current)
        {
            _current = next;
            AvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
