using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
        viewModel.ShowLinked = true;
        var window = new PrintifyListingImportWindow { DataContext = viewModel };

        try
        {
            window.Show();
            var checkboxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.Equal(2, checkboxes.Length);
            Assert.True(checkboxes[0].IsEnabled);
            Assert.True(checkboxes[0].IsTabStop);
            Assert.False(checkboxes[1].IsEnabled);
            var candidatePicker = window.GetVisualDescendants().OfType<ComboBox>().Single(comboBox => comboBox.Items.Count > 0);
            Assert.Single(candidatePicker.Items.Cast<PrintifyListingImportCandidate>());
            Assert.Contains("Other Niche", newProduct.CandidateSummary);
            newProduct.SelectedCandidate = candidate;
            Assert.Equal(candidate.ItemId, newProduct.ConnectToItemId);
            var importButton = window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Import selected"));
            Assert.False(importButton.IsEnabled);
            viewModel.Products[0].IsSelected = true;
            Assert.True(importButton.IsEnabled);
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
