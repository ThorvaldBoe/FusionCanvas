using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.ConceptRefinement;

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
