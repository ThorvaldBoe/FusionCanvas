namespace FusionCanvas.Application.Updates;

public interface IUpdateApplicationLifecycle
{
    Task FlushAsync(CancellationToken cancellationToken = default);

    Task RequestShutdownAsync(CancellationToken cancellationToken = default);
}
