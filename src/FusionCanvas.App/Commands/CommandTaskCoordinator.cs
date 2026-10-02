namespace FusionCanvas.App.Commands;

/// <summary>
/// Owns admission, observation, cancellation, and draining of asynchronous
/// presentation commands during application shutdown.
/// </summary>
public sealed class CommandTaskCoordinator : IDisposable, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly HashSet<Task> _activeTasks = [];
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private readonly Func<Exception, Task>? _recordFailureAsync;
    private int _startsInProgress;
    private bool _admissionClosed;
    private bool _disposed;

    public CommandTaskCoordinator(Func<Exception, Task>? recordFailureAsync = null)
    {
        _recordFailureAsync = recordFailureAsync;
    }

    public CancellationToken CancellationToken => _shutdownCancellation.Token;

    public void Run(Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_gate)
        {
            if (_admissionClosed)
            {
                return;
            }

            _startsInProgress++;
        }

        Task task;
        try
        {
            task = operation(_shutdownCancellation.Token);
        }
        catch
        {
            lock (_gate)
            {
                _startsInProgress--;
            }

            throw;
        }

        var observedTask = ObserveAsync(task);
        lock (_gate)
        {
            _activeTasks.RemoveWhere(activeTask => activeTask.IsCompleted);
            _activeTasks.Add(observedTask);
            _startsInProgress--;
        }
    }

    public void CloseAdmission()
    {
        lock (_gate)
        {
            _admissionClosed = true;
        }
    }

    public void CancelPending() => _shutdownCancellation.Cancel();

    public Task WaitForStartsAsync() => WaitForStartsCoreAsync();

    public Task WaitForTasksAsync() => WaitForTasksCoreAsync();

    public async ValueTask DisposeAsync()
    {
        CloseAdmission();
        _shutdownCancellation.Cancel();
        await WaitForStartsCoreAsync().ConfigureAwait(true);

        Dispose();
        await WaitForTasksCoreAsync().ConfigureAwait(true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseAdmission();
        _shutdownCancellation.Cancel();
    }

    private async Task WaitForStartsCoreAsync()
    {
        while (true)
        {
            lock (_gate)
            {
                if (_startsInProgress == 0)
                {
                    return;
                }
            }

            await Task.Yield();
        }
    }

    private async Task WaitForTasksCoreAsync()
    {
        while (true)
        {
            Task[] pendingTasks;
            bool hasPendingStarts;
            lock (_gate)
            {
                _activeTasks.RemoveWhere(activeTask => activeTask.IsCompleted);
                pendingTasks = _activeTasks.ToArray();
                hasPendingStarts = _startsInProgress > 0;
            }

            if (pendingTasks.Length > 0)
            {
                await Task.WhenAll(pendingTasks).ConfigureAwait(true);
            }
            else if (hasPendingStarts)
            {
                await Task.Yield();
            }
            else
            {
                return;
            }
        }
    }

    private async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (_recordFailureAsync is null)
            {
                return;
            }

            try
            {
                await _recordFailureAsync(exception).ConfigureAwait(true);
            }
            catch
            {
                // Failure reporting must not become a second unobserved command failure.
            }
        }
    }
}
