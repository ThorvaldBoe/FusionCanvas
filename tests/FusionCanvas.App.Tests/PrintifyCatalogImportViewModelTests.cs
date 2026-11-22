using FusionCanvas.App.Stores;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Tests;

public sealed class PrintifyCatalogImportViewModelTests
{
    [Fact]
    public void ImportItemSummarizesBlueprintProvidersAndShopProducts()
    {
        var named = new PrintifyCatalogImportItemViewModel(new(68, "Gildan 64000", 2, [new(9, "SwiftPOD")], ["Product A", "Product B"]));
        var fallback = new PrintifyCatalogImportItemViewModel(new(77, "", 1, [new(12, "Provider 12")], ["Fallback product"]));

        Assert.Equal("Gildan 64000", named.BlueprintName);
        Assert.Contains("SwiftPOD", named.ProviderSummary);
        Assert.Contains("2 shop products", named.ShopProductSummary);
        Assert.Contains("Product B", named.SelectionAccessibleName);
        Assert.Equal("Blueprint 77", fallback.BlueprintName);
    }

    [Fact]
    public async Task LateBlueprintResultForChangedStoreIsIgnored()
    {
        var firstScope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var secondScope = new StoreCredentialScope(firstScope.WorkspaceId, Guid.NewGuid());
        var currentScope = firstScope;
        var pending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { BlueprintsTask = pending.Task };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => currentScope);

        viewModel.Open();
        currentScope = secondScope;
        pending.SetResult(Loaded((68, "Tee")));
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.Empty(viewModel.Blueprints);
        Assert.True(viewModel.IsOpen);
    }

    [Fact]
    public async Task ShowsScanProgressWhileShopProductsAreLoading()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var pending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { ProgressBlueprintTask = pending.Task };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        viewModel.Open();
        await WaitForAsync(() => viewModel.ProgressMessage == "4 of 12 products processed");

        Assert.True(viewModel.IsBusy);
        pending.SetResult(Loaded((68, "Tee")));
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.Null(viewModel.ProgressMessage);
    }

    [Fact]
    public async Task CancellingInFlightLoadReturnsToStableStateAndIgnoresLateResult()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var pending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { BlueprintsTask = pending.Task };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        viewModel.Open();
        await WaitForAsync(() => viewModel.IsBusy);
        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.IsOpen);
        service.LastProgress?.Report(new("Scanning shop products", 5, 20));
        await Task.Yield();
        Assert.Null(viewModel.ProgressMessage);
        pending.SetResult(Loaded((68, "Late")));
        await Task.Yield();

        Assert.Empty(viewModel.Blueprints);
    }

    [Fact]
    public async Task ReopeningSameScopeIgnoresOldResultAndKeepsNewCatalog()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var oldPending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newPending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { BlueprintTasks = new Queue<Task<PrintifyCatalogResult>>([oldPending.Task, newPending.Task]) };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        viewModel.Open();
        await WaitForAsync(() => viewModel.IsBusy);
        viewModel.CancelCommand.Execute(null);
        viewModel.Open();
        await WaitForAsync(() => viewModel.IsBusy);

        oldPending.SetResult(Loaded((68, "Old")));
        newPending.SetResult(Loaded((77, "New")));
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.Single(viewModel.Blueprints);
        Assert.Equal(77, viewModel.Blueprints[0].Id);
    }

    [Fact]
    public async Task CancellingInFlightImportDoesNotInvokeRefresh()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var pending = new TaskCompletionSource<PrintifyCatalogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = false;
        var service = new StubService
        {
            BlueprintsTask = Task.FromResult(Loaded((68, "Tee"))),
            SelectedTask = pending.Task
        };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope, (_, _) => { refreshed = true; return Task.CompletedTask; });

        viewModel.Open();
        await WaitForAsync(() => !viewModel.IsBusy);
        viewModel.Blueprints[0].IsSelected = true;
        viewModel.ConfirmCommand.Execute(null);
        await WaitForAsync(() => viewModel.IsBusy);
        viewModel.CancelCommand.Execute(null);
        pending.SetResult(new(PrintifyCatalogResultKind.Succeeded, "late", SelectedCatalog: []));
        await viewModel.PendingOperation.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsBusy);
        Assert.False(refreshed);
    }

    [Fact]
    public async Task SuccessfulConfirmationRefreshesAndClosesSession()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var refreshed = false;
        var service = new StubService
        {
            BlueprintsTask = Task.FromResult(Loaded((68, "Tee"))),
            SelectedTask = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "imported", SelectedCatalog: []))
        };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope, (_, _) => { refreshed = true; return Task.CompletedTask; });

        viewModel.Open();
        await WaitForAsync(() => !viewModel.IsBusy);
        Assert.Single(viewModel.Blueprints);
        viewModel.Blueprints[0].IsSelected = true;
        viewModel.ConfirmCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.True(refreshed);
        Assert.False(viewModel.IsOpen);
        Assert.Throws<ObjectDisposedException>(() => _ = service.BlueprintsToken.WaitHandle);
        Assert.Throws<ObjectDisposedException>(() => _ = service.SelectedToken.WaitHandle);
    }

    [Fact]
    public async Task DisposeCancelsPendingLoadAndDefersSourceDisposalUntilCompletion()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService();
        service.BlueprintsOperation = async cancellationToken =>
        {
            var waitHandle = cancellationToken.WaitHandle;
            service.BlueprintsWaitHandle = waitHandle;
            started.TrySetResult(cancellationToken);
            try
            {
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                waitHandle.WaitOne();
                cancellationToken.ThrowIfCancellationRequested();
                return Loaded((68, "Late"));
            }
            catch (Exception exception)
            {
                service.BlueprintsException = exception;
                throw;
            }
        };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        try
        {
            viewModel.Open();
            var token = await started.Task.WaitAsync(TestContext.Current.CancellationToken);
            var waitHandle = service.BlueprintsWaitHandle!;

            viewModel.Dispose();

            Assert.True(token.IsCancellationRequested);
            Assert.False(waitHandle.SafeWaitHandle.IsClosed);
            Assert.False(viewModel.CanStart);
            release.TrySetResult();
            await viewModel.PendingOperation.WaitAsync(TestContext.Current.CancellationToken);

            Assert.IsType<OperationCanceledException>(service.BlueprintsException);
            Assert.True(waitHandle.SafeWaitHandle.IsClosed);
            Assert.Empty(viewModel.Blueprints);
        }
        finally
        {
            release.TrySetResult();
            viewModel.Dispose();
            await viewModel.PendingOperation.WaitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task CancellingSelectionDoesNotSubmitAnImport()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var service = new StubService
        {
            BlueprintsTask = Task.FromResult(Loaded((68, "Tee")))
        };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        viewModel.Open();
        await WaitForAsync(() => !viewModel.IsBusy);
        viewModel.Blueprints[0].IsSelected = true;
        Assert.Equal(1, viewModel.SelectedCount);
        Assert.True(viewModel.CanConfirm);

        viewModel.CancelCommand.Execute(null);

        Assert.False(viewModel.IsOpen);
        Assert.Equal(0, service.SelectedCalls);
    }

    [Fact]
    public async Task FailedConfirmationLeavesSelectionOpenAndRecoverable()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var service = new StubService
        {
            BlueprintsTask = Task.FromResult(Loaded((68, "Tee"))),
            SelectedTask = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.PermissionDenied, "Printify rejected the request."))
        };
        var viewModel = new PrintifyCatalogImportViewModel(service, () => scope);

        viewModel.Open();
        await WaitForAsync(() => !viewModel.IsBusy);
        viewModel.Blueprints[0].IsSelected = true;
        viewModel.ConfirmCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.True(viewModel.IsOpen);
        Assert.Equal("Printify rejected the request.", viewModel.ErrorMessage);
        Assert.True(viewModel.CanConfirm);
    }

    private static async Task WaitForAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 100 && !predicate(); attempt++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.True(predicate());
    }

    private static PrintifyCatalogResult Loaded(params (int Id, string Name)[] blueprints) =>
        new(PrintifyCatalogResultKind.Succeeded, "loaded", ShopBlueprints: blueprints.Select(value =>
            new PrintifyShopBlueprintSummary(value.Id, value.Name, 1, [new(9, "Provider 9")], ["Shop product"])).ToArray());

    private sealed class StubService : IPrintifyCatalogImportService
    {
        public Task<PrintifyCatalogResult> BlueprintsTask { get; init; } = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "empty", []));
        public Task<PrintifyCatalogResult>? ProgressBlueprintTask { get; init; }
        public Queue<Task<PrintifyCatalogResult>>? BlueprintTasks { get; init; }
        public Func<CancellationToken, Task<PrintifyCatalogResult>>? BlueprintsOperation { get; set; }
        public WaitHandle? BlueprintsWaitHandle { get; set; }
        public Exception? BlueprintsException { get; set; }
        public Task<PrintifyCatalogResult> SelectedTask { get; init; } = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "empty", []));
        public int SelectedCalls { get; private set; }
        public CancellationToken BlueprintsToken { get; private set; }
        public CancellationToken SelectedToken { get; private set; }
        public IProgress<PrintifyCatalogImportProgress>? LastProgress { get; private set; }

        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            BlueprintsToken = cancellationToken;
            return BlueprintsOperation?.Invoke(cancellationToken)
                ?? (BlueprintTasks is null ? BlueprintsTask : BlueprintTasks.Dequeue());
        }

        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken, IProgress<PrintifyCatalogImportProgress>? progress)
        {
            LastProgress = progress;
            progress?.Report(new("Scanning shop products", 4, 12));
            return ProgressBlueprintTask ?? LoadBlueprintsAsync(scope, cancellationToken);
        }

        public Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default)
        {
            SelectedCalls++;
            SelectedToken = cancellationToken;
            return SelectedTask;
        }
    }
}
