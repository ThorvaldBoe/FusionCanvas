using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Products;

namespace FusionCanvas.App.Tests;

public class StoreEditorHeadlessTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public void ProductsTabButton_SelectsProductsTabAndShowsPanel()
    {
        var window = CreateEditorWindow();

        var button = FindButton(window, "Products & fulfillment");
        Assert.NotNull(button);

        button!.Command!.Execute(button.CommandParameter);
        window.UpdateLayout();
        window.UpdateLayout();

        var viewModel = (StoreManagementViewModel)window.DataContext!;
        Assert.True(viewModel.IsProductsTabSelected);

        var newProductButton = FindButton(window, "New product");
        Assert.NotNull(newProductButton);
        Assert.True(newProductButton!.IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void ProductsPanel_HasNewProductActionForActiveStore()
    {
        var window = CreateEditorWindow();

        var viewModel = (StoreManagementViewModel)window.DataContext!;
        window.UpdateLayout();
        window.UpdateLayout();

        viewModel.SelectProductsTabCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var newProductButton = FindButton(window, "New product");
        Assert.NotNull(newProductButton);
        Assert.True(newProductButton!.IsEnabled);

        window.Close();
    }

    private static StoreEditorWindow CreateEditorWindow()
    {
        var store = new Store(Guid.NewGuid(), "North Star", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(Snapshot(store));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new ProductSupplierSetupService(repository));
        viewModel.LoadAsync(default).GetAwaiter().GetResult();
        var window = new StoreEditorWindow { DataContext = viewModel };
        window.Show();
        window.UpdateLayout();
        window.UpdateLayout();
        return window;
    }

    private static Button? FindButton(Window window, string content) =>
        window.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => string.Equals(b.Content as string, content, System.StringComparison.Ordinal));

    private static WorkspaceSnapshot Snapshot(Store store)
    {
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Gildan 64000", null, null, Now, Now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        return new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [store],
            [],
            [],
            [item],
            [],
            [],
            [],
            [],
            [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering]
        };
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
