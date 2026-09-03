using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.SllGeneration;

public sealed class ConfiguredSllAccessStatus(IAiTextGenerationService ai)
    : ISllAccessStatus
{
    private readonly IAiTextGenerationService _ai =
        ai ?? throw new ArgumentNullException(nameof(ai));
    private SllAccessAvailability _current =
        SllAccessAvailability.Unavailable("Checking AI configuration…");

    public event EventHandler? AvailabilityChanged;

    public SllAccessAvailability GetAvailability() => _current;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var availability = await _ai
            .GetAvailabilityAsync(AiRequestPurpose.Sll, cancellationToken)
            .ConfigureAwait(false);
        var next = availability.IsReady
            ? SllAccessAvailability.Available
            : SllAccessAvailability.Unavailable(availability.Message);
        if (next != _current)
        {
            _current = next;
            AvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
