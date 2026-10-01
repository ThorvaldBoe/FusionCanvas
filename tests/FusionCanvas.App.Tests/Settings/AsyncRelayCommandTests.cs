using FusionCanvas.App.Settings;

namespace FusionCanvas.App.Tests.Settings;

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
}
