using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public sealed class PrintifyListingImportWindowTests
{
    [AvaloniaFact]
    public void Dialog_renders_products_and_disables_already_linked_product_selection()
    {
        var viewModel = new PrintifyListingImportViewModel(null!, new(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());
        var newProduct = viewModel.AddProduct(new(new("new", "New shirt", "Description", true, 68, 9), false, []));
        var candidate = new PrintifyListingImportCandidate(Guid.NewGuid(), "Existing Item", "Local description", "Store / Other Niche", true, 0.95, 0.92);
        newProduct.Candidates = [candidate];
        viewModel.AddProduct(new(new("linked", "Linked shirt", "Description", false, 68, 9), true, []));
        var window = new PrintifyListingImportWindow { DataContext = viewModel };

        try
        {
            window.Show();
            var checkboxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.Single(checkboxes);
            Assert.True(checkboxes[0].IsEnabled);
            Assert.True(checkboxes[0].IsTabStop);
            var candidatePicker = window.GetVisualDescendants().OfType<ComboBox>().Single(comboBox => comboBox.Items.Count > 0);
            Assert.Single(candidatePicker.Items.Cast<PrintifyListingImportCandidate>());
            Assert.Contains("Other Niche", newProduct.CandidateSummary);
            newProduct.SelectedCandidate = candidate;
            Assert.Equal(candidate.ItemId, newProduct.ConnectToItemId);
            var importButton = window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Import selected"));
            Assert.False(importButton.IsEnabled);
            viewModel.Products[0].IsSelected = true;
            Assert.True(importButton.IsEnabled);

            var linkedToggle = window.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Show linked products");
            Assert.Equal("Show linked products", linkedToggle.Content);
            Assert.Single(window.GetVisualDescendants().OfType<CheckBox>());

            linkedToggle.Command!.Execute(linkedToggle.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Hide linked products", linkedToggle.Content);
            Assert.Equal("Hide linked products", AutomationProperties.GetName(linkedToggle));
            checkboxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.Equal(2, checkboxes.Length);
            Assert.True(checkboxes[0].IsEnabled);
            Assert.False(checkboxes[1].IsEnabled);
            Assert.False(viewModel.Products[1].IsSelected);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), textBlock => textBlock.Text == "Already linked" && textBlock.IsVisible);
            Assert.True(viewModel.CanImport);
            newProduct.IsSelected = false;
            viewModel.Products[1].IsSelected = true;
            Assert.False(viewModel.CanImport);

            linkedToggle.Command.Execute(linkedToggle.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Show linked products", linkedToggle.Content);
            Assert.Equal("Show linked products", AutomationProperties.GetName(linkedToggle));
            Assert.Single(window.GetVisualDescendants().OfType<CheckBox>());
        }
        finally { window.Close(); }
    }

    [Fact]
    public void Import_menu_availability_is_limited_to_single_niche_selection()
    {
        var niche = new WorkspaceTreeNodeViewModel(Guid.NewGuid(), WorkspaceEntityKind.Niche, Guid.NewGuid(), "Niche", null, true, false, 0, []);
        var group = new WorkspaceTreeNodeViewModel(Guid.NewGuid(), WorkspaceEntityKind.Group, Guid.NewGuid(), "Group", null, true, false, 0, []);

        Assert.True(niche.IsNicheAndSingleSelection);
        Assert.False(group.IsNicheAndSingleSelection);
        niche.HasMultiSelectionContext = true;
        Assert.False(niche.IsNicheAndSingleSelection);
    }
}
