using FusionCanvas.App.Commands;

namespace FusionCanvas.App.Tests.Commands;

public sealed class AsyncRelayCommandTests
{
    [Fact]
    public async Task ExecuteExposesSuccessfulExecutionAndReleasesBusyState()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(() => release.Task);

        command.Execute(null);

        Assert.NotNull(command.ExecutionTask);
        var execution = command.ExecutionTask!;
        Assert.False(execution.IsCompleted);
        Assert.False(command.CanExecute(null));

        release.SetResult();
        await execution;

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ExecuteExposesDelegateFailureWithoutThrowingFromICommandDispatch()
    {
        var failure = new InvalidOperationException("execution failed");
        var command = new AsyncRelayCommand(() => Task.FromException(failure));

        command.Execute(null);

        Assert.NotNull(command.ExecutionTask);
        var execution = command.ExecutionTask!;
        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => execution);

        Assert.Same(failure, observed);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ExecuteExposesCancellationAndReleasesBusyState()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var command = new AsyncRelayCommand(() => Task.FromCanceled(cancellation.Token));

        command.Execute(null);

        Assert.NotNull(command.ExecutionTask);
        var execution = command.ExecutionTask!;
        Assert.True(execution.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ExecuteIgnoresReentryWhileExecutionIsInProgress()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            executionCount++;
            await release.Task;
        });

        command.Execute(null);
        var execution = command.ExecutionTask!;

        command.Execute(null);

        Assert.Equal(1, executionCount);
        Assert.Same(execution, command.ExecutionTask);
        Assert.False(command.CanExecute(null));

        release.SetResult();
        await execution;

        Assert.Equal(1, executionCount);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ExecuteRaisesCanExecuteChangedWhenBusyStateChanges()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(() => release.Task);
        var canExecuteStates = new List<bool>();

        command.CanExecuteChanged += (_, _) => canExecuteStates.Add(command.CanExecute(null));

        command.Execute(null);

        Assert.Equal([false], canExecuteStates);

        release.SetResult();
        await command.ExecutionTask!;

        Assert.Equal([false, true], canExecuteStates);
    }
}
