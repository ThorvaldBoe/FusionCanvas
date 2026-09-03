using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public interface IIdeationAccessStatus
{
    event EventHandler? AvailabilityChanged
    {
        add { }
        remove { }
    }

    IdeationAccessAvailability GetAvailability();

    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
