using Avalonia.Controls;
using Avalonia.Automation;
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

    [AvaloniaFact]
    public void ProductsPanel_DisclosesProductAndOfferingActionsByLevel()
    {
        var window = CreateEditorWindow();
        var viewModel = (StoreManagementViewModel)window.DataContext!;

        viewModel.SelectProductsTabCommand.Execute(null);
        window.UpdateLayout();
        Assert.True(viewModel.IsCatalogOverview);
        Assert.NotNull(FindButton(window, "New product"));
        Assert.Null(FindButton(window, "Add fulfillment offering"));

        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        window.UpdateLayout();
        Assert.True(viewModel.IsProductDetail);
        Assert.NotNull(FindButton(window, "Add fulfillment offering"));
        Assert.Null(FindButton(window, "Add variant"));

        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();
        Assert.True(viewModel.IsOfferingDetail);
        Assert.NotNull(FindButton(window, "Add variant"));
        Assert.NotNull(FindButton(window, "Add printable area"));

        window.Close();
    }

    [AvaloniaFact]
    public void StoreCreationControls_ExposeStableAutomationIdentifiers()
    {
        var window = CreateEditorWindow();

        var newStore = FindButton(window, "New store");
        var storeName = window.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.Name == "StoreNameTextBox");
        var save = FindButton(window, "Save");
        var activeStores = window.GetVisualDescendants().OfType<ItemsControl>()
            .Single(control => AutomationProperties.GetAutomationId(control) == "StoreEditor.ActiveStores");

        Assert.Equal("StoreEditor.NewStore", AutomationProperties.GetAutomationId(newStore));
        Assert.Equal("StoreEditor.Name", AutomationProperties.GetAutomationId(storeName));
        Assert.Equal("StoreEditor.SaveStore", AutomationProperties.GetAutomationId(save));
        Assert.NotNull(activeStores);

        window.Close();
    }

    [AvaloniaFact]
    public void NicheDetailsFields_KeepTrailingMargin()
    {
        var window = CreateEditorWindow();

        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectNichesTabCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var nicheFields = window.GetVisualDescendants()
            .OfType<TextBox>()
            .Where(textBox => textBox.IsVisible &&
                textBox.PlaceholderText is not null &&
                textBox.PlaceholderText is
                    "Niche name" or
                    "Description" or
                    "Audience" or
                    "Humor style" or
                    "Visual style guidance" or
                    "Constraints" or
                    "Risks" or
                    "Research notes" or
                    "Notes")
            .ToArray();

        Assert.NotEmpty(nicheFields);
        Assert.All(nicheFields, textBox =>
        {
            var parent = Assert.IsAssignableFrom<Control>(textBox.Parent);
            Assert.True(parent.Bounds.Width - textBox.Bounds.Right >= 16,
                $"The {textBox.PlaceholderText} field should have a trailing margin.");
        });

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
            .FirstOrDefault(b => IsEffectivelyVisible(b) &&
                string.Equals(b.Content as string, content, System.StringComparison.Ordinal));

    private static bool IsEffectivelyVisible(Control control)
    {
        for (Control? current = control; current is not null; current = current.Parent as Control)
        {
            if (!current.IsVisible)
            {
                return false;
            }
        }

        return true;
    }

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
