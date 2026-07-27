namespace FusionCanvas.App;

internal static class StartupTaskRunner
{
    public static T Run<T>(Func<Task<T>> operation) =>
        Task.Run(operation).GetAwaiter().GetResult();

    public static void Run(Func<Task> operation) =>
        Task.Run(operation).GetAwaiter().GetResult();
}
