using FusionCanvas.App.Commands;

namespace FusionCanvas.App.Tests.Commands;

public sealed class CommandTaskCoordinatorTests
{
    [Fact]
    public async Task Run_observes_failures_without_leaking_them_to_the_command_source()
    {
        Exception? recorded = null;
        await using var coordinator = new CommandTaskCoordinator(exception =>
        {
            recorded = exception;
            return Task.CompletedTask;
        });

        var failure = new InvalidOperationException("command failed");
        coordinator.Run(_ => Task.FromException(failure));

        await coordinator.WaitForTasksAsync();

        Assert.Same(failure, recorded);
    }

    [Fact]
    public async Task Close_admission_prevents_new_commands()
    {
        await using var coordinator = new CommandTaskCoordinator();
        var started = false;

        coordinator.CloseAdmission();
        coordinator.Run(_ =>
        {
            started = true;
            return Task.CompletedTask;
        });

        await coordinator.WaitForTasksAsync();

        Assert.False(started);
    }

    [Fact]
    public async Task Dispose_async_cancels_and_drains_pending_commands()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new CommandTaskCoordinator();

        coordinator.Run(async cancellationToken =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled.SetResult();
            }
        });

        await started.Task;
        await coordinator.DisposeAsync();

        Assert.True(cancelled.Task.IsCompletedSuccessfully);
    }
}
