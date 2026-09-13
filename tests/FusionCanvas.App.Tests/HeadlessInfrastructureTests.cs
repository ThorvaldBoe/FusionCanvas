using FusionCanvas.Domain.Workspace;
using Avalonia.Headless.XUnit;
using Xunit;

namespace FusionCanvas.App.Tests;

public sealed class HeadlessInfrastructureTests
{
    [AvaloniaFact]
    public async Task Wait_completes_immediately_when_condition_is_true()
    {
        await TestSupport.HeadlessUiWait.UntilAsync(() => true, "immediate state");
    }

    [AvaloniaFact]
    public async Task Wait_observes_state_published_by_a_continuation()
    {
        var published = false;
        _ = Task.Run(async () =>
        {
            await Task.Yield();
            published = true;
        });

        await TestSupport.HeadlessUiWait.UntilAsync(() => published, "published state");
        Assert.True(published);
    }

    [AvaloniaFact]
    public async Task Wait_honors_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            TestSupport.HeadlessUiWait.UntilAsync(() => false, "cancelled state", TimeSpan.FromSeconds(1), cancellation.Token));
    }

    [AvaloniaFact]
    public async Task Wait_timeout_names_the_observable_condition()
    {
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TestSupport.HeadlessUiWait.UntilAsync(() => false, "selected shop is visible", TimeSpan.FromMilliseconds(10)));

        Assert.Contains("selected shop is visible", exception.Message);
    }

    [Fact]
    public async Task Disposable_workspaces_are_isolated_and_do_not_use_configured_paths()
    {
        using var first = new TestSupport.DisposableHeadlessWorkspace();
        using var second = new TestSupport.DisposableHeadlessWorkspace();
        Assert.NotEqual(first.DatabasePath, second.DatabasePath);
        Assert.NotEqual(first.WorkspacePath, second.WorkspacePath);
        Assert.NotEqual(first.SettingsPath, second.SettingsPath);
        Assert.DoesNotContain(".fusioncanvas", first.RootPath, StringComparison.OrdinalIgnoreCase);

        var snapshot = TestSupport.SampleWorkspace.Create();
        await first.CreateRepository().SaveAsync(snapshot);
        var secondSnapshot = await second.CreateRepository().LoadAsync();
        Assert.Empty(secondSnapshot.Stores);
    }
}
