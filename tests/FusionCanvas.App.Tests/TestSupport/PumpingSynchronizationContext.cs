using System.Collections.Concurrent;

namespace FusionCanvas.App.Tests.TestSupport;

internal sealed class PumpingSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _callbacks = new();

    public override void Post(SendOrPostCallback callback, object? state) => _callbacks.Add((callback, state));

    public override SynchronizationContext CreateCopy() => this;

    public void PumpUntil(Task task, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!task.IsCompleted)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException("The asynchronous operation did not complete while pumping its captured context.");
            }

            if (_callbacks.TryTake(out var callback, remaining < TimeSpan.FromMilliseconds(50)
                    ? remaining
                    : TimeSpan.FromMilliseconds(50)))
            {
                callback.Callback(callback.State);
            }
        }

        task.GetAwaiter().GetResult();
        while (_callbacks.TryTake(out var callback))
        {
            callback.Callback(callback.State);
        }
    }

    public void Dispose() => _callbacks.Dispose();
}
