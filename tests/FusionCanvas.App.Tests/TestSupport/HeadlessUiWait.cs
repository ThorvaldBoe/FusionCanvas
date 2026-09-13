using Avalonia.Threading;

namespace FusionCanvas.App.Tests.TestSupport;

/// <summary>Deterministically settles Avalonia state without arbitrary wall-clock sleeps.</summary>
internal static class HeadlessUiWait
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    internal static async Task UntilAsync(
        Func<bool> condition,
        string description,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dispatcher.UIThread.RunJobs();
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for '{description}'. Dispatcher jobs were pumped; the observable condition never became true.");
            }

            await Task.Yield();
        }

        Dispatcher.UIThread.RunJobs();
    }
}
