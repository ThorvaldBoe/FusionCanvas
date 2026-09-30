namespace FusionCanvas.App;

internal static class StartupTaskRunner
{
    public static T Run<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Task.Run(() => operation(cancellationToken), cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public static void Run(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Task.Run(() => operation(cancellationToken), cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
}
