using FusionCanvas.Application.Updates;

namespace FusionCanvas.App.Updates;

public sealed class AppUpdateApplicationLifecycle : IUpdateApplicationLifecycle
{
    private readonly Func<Task> _flush;
    private readonly Action _requestShutdown;

    public AppUpdateApplicationLifecycle(Func<Task> flush, Action requestShutdown)
    {
        _flush = flush ?? throw new ArgumentNullException(nameof(flush));
        _requestShutdown = requestShutdown ?? throw new ArgumentNullException(nameof(requestShutdown));
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _flush();
    }

    public Task RequestShutdownAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requestShutdown();
        return Task.CompletedTask;
    }
}
