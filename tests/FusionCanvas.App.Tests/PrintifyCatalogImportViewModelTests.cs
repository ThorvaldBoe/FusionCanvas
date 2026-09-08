using FusionCanvas.App.Stores;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Tests;

public sealed class PrintifyCatalogImportViewModelTests
{
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
        pending.SetResult(new(PrintifyCatalogResultKind.Succeeded, "loaded", [new(68, "Tee", null, null, null)]));
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.Empty(viewModel.Blueprints);
        Assert.True(viewModel.IsOpen);
    }

    [Fact]
    public async Task SuccessfulConfirmationRefreshesAndClosesSession()
    {
        var scope = new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid());
        var refreshed = false;
        var service = new StubService
        {
            BlueprintsTask = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "loaded", [new(68, "Tee", null, null, null)])),
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
    }

    private static async Task WaitForAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 100 && !predicate(); attempt++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.True(predicate());
    }

    private sealed class StubService : IPrintifyCatalogImportService
    {
        public Task<PrintifyCatalogResult> BlueprintsTask { get; init; } = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "empty", []));
        public Task<PrintifyCatalogResult> SelectedTask { get; init; } = Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "empty", []));
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) => BlueprintsTask;
        public Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => SelectedTask;
    }
}
