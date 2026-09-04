using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.SllGeneration;

public interface ISllAccessStatus
{
    event EventHandler? AvailabilityChanged
    {
        add { }
        remove { }
    }

    SllAccessAvailability GetAvailability();

    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
