namespace FusionCanvas.App.Tests;

public sealed class StartupTaskRunnerTests
{
    [Fact]
    public void Run_PassesTheOwnedStartupTokenToTheOperation()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken observed = default;

        StartupTaskRunner.Run(
            token =>
            {
                observed = token;
                return Task.FromResult(true);
            },
            cancellation.Token);

        Assert.Equal(cancellation.Token, observed);
    }

    [Fact]
    public void Run_WhenStartupIsStopped_DoesNotStartTheOperation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var invoked = false;

        Assert.ThrowsAny<OperationCanceledException>(() => StartupTaskRunner.Run(
            token =>
            {
                invoked = true;
                return Task.FromResult(true);
            },
            cancellation.Token));

        Assert.False(invoked);
    }

    [Fact]
    public async Task Run_WhenStartupIsStopped_CancelsTheRunningOperation()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = Task.Run(
            () => StartupTaskRunner.Run(
                async token =>
                {
                    started.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return true;
                },
                cancellation.Token),
            TestContext.Current.CancellationToken);

        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => run.WaitAsync(TestContext.Current.CancellationToken));
    }
}
