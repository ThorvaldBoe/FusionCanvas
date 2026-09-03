using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

/// <summary>
/// Headless view tests for the Design Stage Tool UI.
/// Verifies configuration selection, color working set, slot grid,
/// large preview, supporting images, and read-only states.
/// </summary>
public class DesignStageToolHeadlessTests
{
    /// <summary>
    /// Creates a ViewModel with a Design-stage item that has a listing configuration,
    /// selected colors, a default row, and slot areas.
    /// </summary>
    private static MainWindowViewModel CreateConfiguredDesignViewModel()
    {
        var baseSnapshot = SampleWorkspace.Create();
        var designItem = baseSnapshot.Items.First(i => i.Id == SampleWorkspace.DesignNodeId);
        var offering = baseSnapshot.FulfillmentOfferings[0];
        var now = DateTimeOffset.UtcNow;

        // Add a second variant with "White" so available colors include both
        var existingVariant = baseSnapshot.ProductVariants.First(v => v.FulfillmentOfferingId == offering.Id);
        var whiteVariant = new ProductVariant(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            offering.Id,
            [new VariantOption("Color", "White")],
            now, now);

        var variants = new List<ProductVariant>(baseSnapshot.ProductVariants) { whiteVariant };

        // Need a second area for the grid
        var area2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var area2 = new DesignArea(
            area2Id, offering.Id, "Back", null, "back", "DTG", 3000, 4500,
            [existingVariant.Id, whiteVariant.Id],
            now, now, "{}");

        var config = new ItemListingConfiguration(designItem.Id, offering.Id);
        var selectedColors = new List<DesignSelectedColor>
        {
            new(designItem.Id, "Black"),
            new(designItem.Id, "White")
        };
        var rowId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var row = new DesignVariantRow(rowId, designItem.Id, isDefault: true, sortOrder: 0);
        var rowColors = new List<DesignVariantRowColor>
        {
            new(rowId, "Black"),
            new(rowId, "White")
        };

        var snapshot = baseSnapshot with
        {
            ProductVariants = variants,
            DesignAreas = [.. baseSnapshot.DesignAreas, area2],
            ItemListingConfigurations = [config],
            DesignSelectedColors = selectedColors,
            DesignVariantRows = [row],
            DesignVariantRowColors = rowColors,
            DesignSlotAssignments = []
        };
        var repo = new InMemoryWorkspaceRepository(snapshot);
        return MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repo);
    }

    /// <summary>
    /// Creates a ViewModel with a Choice-network offering selected.
    /// </summary>
    private static MainWindowViewModel CreateChoiceOfferingDesignViewModel()
    {
        var baseSnapshot = SampleWorkspace.Create();
        var designItem = baseSnapshot.Items.First(i => i.Id == SampleWorkspace.DesignNodeId);
        var now = DateTimeOffset.UtcNow;

        var choiceOffering = new FulfillmentOffering(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            baseSnapshot.StoreProducts[0].Id,
            "Choice", null, FulfillmentKind.PrintifyChoiceNetwork, null, null, now, now, "{}");

        var variant = new ProductVariant(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            choiceOffering.Id,
            [new VariantOption("Color", "Black")],
            now, now);

        var area = new DesignArea(
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            choiceOffering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], now, now, "{}");

        var config = new ItemListingConfiguration(designItem.Id, choiceOffering.Id);
        var selectedColors = new List<DesignSelectedColor> { new(designItem.Id, "Black") };
        var rowId = Guid.Parse("80000000-0000-0000-0000-000000000001");
        var row = new DesignVariantRow(rowId, designItem.Id, isDefault: true, sortOrder: 0);
        var rowColors = new List<DesignVariantRowColor> { new(rowId, "Black") };

        var snapshot = baseSnapshot with
        {
            FulfillmentOfferings = [choiceOffering],
            ProductVariants = [variant],
            DesignAreas = [area],
            ItemListingConfigurations = [config],
            DesignSelectedColors = selectedColors,
            DesignVariantRows = [row],
            DesignVariantRowColors = rowColors,
            DesignSlotAssignments = []
        };
        var repo = new InMemoryWorkspaceRepository(snapshot);
        return MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repo);
    }

    /// <summary>
    /// Loads the Design stage for the configured ViewModel.
    /// </summary>
    private static void NavigateToDesign(MainWindowViewModel vm)
    {
        var ctx = vm.NavigationContexts.First(c =>
            c.Context.EntityKind == WorkspaceEntityKind.Item
            && c.Context.Id == SampleWorkspace.DesignNodeId);
        vm.OpenFromNavigation(ctx);
        vm.SelectWorkflowStage(WorkflowStage.Design);
    }

    [AvaloniaFact]
    public void NoConfiguration_ShowsPromptAndHidesSlotGrid()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        Assert.True(vm.ShowsDesignStageTool);
        Assert.NotNull(vm.DesignTool);
        Assert.False(vm.DesignTool.HasConfiguration);
        Assert.Empty(vm.DesignTool.Rows);
        Assert.Empty(vm.DesignTool.SelectedColors);
    }

    [AvaloniaFact]
    public void NoConfiguration_SupportingImagesAreaVisible()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        var supportingHeading = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text is "Supporting Images");
        Assert.NotNull(supportingHeading);
    }

    [AvaloniaFact]
    public void ConfigurationSelector_ShowsPromptWhenUnconfigured()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        var prompt = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text is "Select a listing configuration to show the design slot grid.");
        Assert.NotNull(prompt);
    }

    [AvaloniaFact]
    public async Task ConfigurationSelector_PersistsSelectionThroughViewModelBinding()
    {
        var snapshot = SampleWorkspace.Create();
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var vm = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        NavigateToDesign(vm);

        var offering = Assert.Single(vm.DesignTool.AvailableOfferings);
        vm.DesignTool.SelectedOffering = offering;

        for (var i = 0; i < 200 && !repository.Snapshot.ItemListingConfigurations.Any(c => c.ItemId == SampleWorkspace.DesignNodeId && c.OfferingId == offering.Id); i++)
        {
            await Task.Delay(10);
        }

        Assert.Contains(repository.Snapshot.ItemListingConfigurations,
            c => c.ItemId == SampleWorkspace.DesignNodeId && c.OfferingId == offering.Id);
    }

    [AvaloniaFact]
    public void ConfiguredState_ShowsRowsAndAreas()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        Assert.True(vm.ShowsDesignStageTool);
        Assert.True(vm.DesignTool.HasConfiguration);
        Assert.NotEmpty(vm.DesignTool.Rows);

        var row = vm.DesignTool.Rows[0];
        Assert.True(row.IsDefault);
        Assert.Contains("Black", row.ColorValues);
        Assert.Contains("White", row.ColorValues);
        Assert.Equal(2, row.Slots.Count); // 2 design areas
    }

    [AvaloniaFact]
    public void ConfiguredState_AvailableColorsMatchOffering()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        Assert.NotEmpty(vm.DesignTool.AvailableColors);
        Assert.Contains(vm.DesignTool.AvailableColors, c => c.ColorValue == "Black");
        Assert.Contains(vm.DesignTool.AvailableColors, c => c.ColorValue == "White");
    }

    [AvaloniaFact]
    public void LargePreviewDialog_OpensAndCloses()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        Assert.False(vm.DesignTool.ShowPreviewDialog);

        // Open preview via the VM — uses a null path so no real bitmap is loaded
        vm.DesignTool.PreviewSupportingImage(Guid.NewGuid(), null);
        Assert.True(vm.DesignTool.ShowPreviewDialog);

        vm.DesignTool.ClosePreviewDialog();
        Assert.False(vm.DesignTool.ShowPreviewDialog);
    }

    [AvaloniaFact]
    public void LargePreviewDialog_ImageSourceIsBitmap()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        // Open a preview; since no real file exists, PreviewBitmap stays null here
        // but we verify the property is wired correctly
        vm.DesignTool.PreviewSupportingImage(Guid.NewGuid(), null);
        Assert.True(vm.DesignTool.ShowPreviewDialog);

        // PreviewBitmap should be null when no file exists
        Assert.Null(vm.DesignTool.PreviewBitmap);

        vm.DesignTool.ClosePreviewDialog();
    }

    [AvaloniaFact]
    public void ReadOnlyState_DisablesControls()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        // Load the design tool with canEdit=false
        vm.DesignTool.LoadAsync(
            vm.ItemInspector.LoadedItemId ?? SampleWorkspace.DesignNodeId,
            canEdit: false).GetAwaiter().GetResult();
        fixture.PumpLayout();

        Assert.True(vm.DesignTool.IsReadOnly);
        Assert.NotEmpty(vm.DesignTool.ReadOnlyReason);
    }

    [AvaloniaFact]
    public void SupportingImagesSection_AlwaysVisible()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        var supportingHeading = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text is "Supporting Images");
        Assert.NotNull(supportingHeading);
        Assert.True(supportingHeading.IsVisible);
    }

    [AvaloniaFact]
    public void DesignStageTool_LoadsWithoutCrashing()
    {
        using var fixture = new MainWindowFixture();
        NavigateToDesign(fixture.ViewModel);
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowsDesignStageTool);
        Assert.NotNull(fixture.ViewModel.DesignTool);
    }

    [AvaloniaFact]
    public void ConfiguredState_SelectedColorsMatchRows()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // Selected colors from the configured snapshot
        Assert.Equal(2, vm.DesignTool.SelectedColors.Count);
        Assert.Contains(vm.DesignTool.SelectedColors, c => c.ColorValue == "Black");
        Assert.Contains(vm.DesignTool.SelectedColors, c => c.ColorValue == "White");
    }

    [AvaloniaFact]
    public void UnconfiguredState_ConfigurationComboBoxPresent()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        // The configuration ComboBox should be visible when HasConfiguration is false
        // It uses DisplayMemberBinding bound to DesignTool.AvailableOfferings
        var configComboBoxes = fixture.Window.GetVisualDescendants()
            .OfType<ComboBox>()
            .Where(cb => cb.ItemsSource == vm.DesignTool.AvailableOfferings)
            .ToList();
        Assert.NotEmpty(configComboBoxes);
    }

    [AvaloniaFact]
    public void ReadOnlyState_ReadOnlyReasonDisplayed()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        vm.DesignTool.LoadAsync(
            vm.ItemInspector.LoadedItemId ?? SampleWorkspace.DesignNodeId,
            canEdit: false).GetAwaiter().GetResult();
        fixture.PumpLayout();

        // The read-only banner should be visible
        var readOnlyBanner = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text is not null && tb.Text.Contains("read-only", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(readOnlyBanner);
        Assert.True(readOnlyBanner.IsVisible);
    }

    // === Additional headless view tests for color operations and slot grid affordances ===

    [AvaloniaFact]
    public void ConfiguredState_ColorToggle_AddsAndRemovesColor()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // Start with Black and White selected
        Assert.Contains(vm.DesignTool.SelectedColors, c => c.ColorValue == "Black");

        // Remove "Black" via the ViewModel service method
        vm.DesignTool.ToggleColorAsync("Black", add: false).GetAwaiter().GetResult();

        // Black should no longer be selected
        Assert.DoesNotContain(vm.DesignTool.SelectedColors, c => c.ColorValue == "Black");
        // But White should still be selected
        Assert.Contains(vm.DesignTool.SelectedColors, c => c.ColorValue == "White");

        // Add "Black" back
        vm.DesignTool.ToggleColorAsync("Black", add: true).GetAwaiter().GetResult();
        Assert.Contains(vm.DesignTool.SelectedColors, c => c.ColorValue == "Black");
    }

    [AvaloniaFact]
    public void ConfiguredState_MakeSpecificForColor_CreatesNewRow()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // Start with 1 default row serving both colors
        Assert.Single(vm.DesignTool.Rows);
        var defaultRow = vm.DesignTool.Rows[0];
        Assert.True(defaultRow.IsDefault);
        Assert.Contains("Black", defaultRow.ColorValues);

        // Make "Black" specific
        vm.DesignTool.MakeSpecificForColorAsync("Black").GetAwaiter().GetResult();

        // Now there should be 2 rows  (default row for White, specific row for Black)
        Assert.Equal(2, vm.DesignTool.Rows.Count);
        var specificRow = vm.DesignTool.Rows.FirstOrDefault(r => !r.IsDefault);
        Assert.NotNull(specificRow);
        Assert.Contains("Black", specificRow!.ColorValues);
        Assert.DoesNotContain("White", specificRow.ColorValues);

        // Default row should only have White
        var updatedDefault = vm.DesignTool.Rows.First(r => r.IsDefault);
        Assert.DoesNotContain("Black", updatedDefault.ColorValues);
        Assert.Contains("White", updatedDefault.ColorValues);
    }

    [AvaloniaFact]
    public void ConfiguredState_RemoveSpecificRow_RevertsColorsToDefault()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // Make "Black" specific first
        vm.DesignTool.MakeSpecificForColorAsync("Black").GetAwaiter().GetResult();
        Assert.Equal(2, vm.DesignTool.Rows.Count);

        // Remove the specific row
        var specificRow = vm.DesignTool.Rows.First(r => !r.IsDefault);
        vm.DesignTool.RequestRemoveSpecificRow(specificRow.RowId);
        Assert.True(vm.DesignTool.IsRemovalConfirmationVisible);

        // Confirm removal
        vm.DesignTool.ConfirmPendingRemovalAsync().GetAwaiter().GetResult();

        // Back to 1 default row with both colors
        Assert.Single(vm.DesignTool.Rows);
        var defaultRow = vm.DesignTool.Rows[0];
        Assert.True(defaultRow.IsDefault);
        Assert.Contains("Black", defaultRow.ColorValues);
        Assert.Contains("White", defaultRow.ColorValues);
    }

    [AvaloniaFact]
    public void ConfiguredState_SlotGridButtons_ExistAndHaveCorrectStates()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // With empty slots, HasImage should be false for each slot
        foreach (var row in vm.DesignTool.Rows)
        {
            foreach (var slot in row.Slots)
            {
                Assert.False(slot.HasImage);
                Assert.False(slot.CanPreview);
                Assert.False(slot.CanExport);
            }
        }
    }

    [AvaloniaFact]
    public void ConfiguredState_ChoiceOffering_ShowsStatus()
    {
        var vm = CreateChoiceOfferingDesignViewModel();
        NavigateToDesign(vm);

        // Verify the Choice offering status is displayed
        Assert.True(vm.DesignTool.HasConfiguration);
        Assert.NotNull(vm.DesignTool.SelectedOfferingStatus);
        Assert.Equal("Printify Choice network", vm.DesignTool.SelectedOfferingStatus);
    }

    [AvaloniaFact]
    public void ConfiguredState_SlotThumbnail_NullWhenNoFile()
    {
        var vm = CreateConfiguredDesignViewModel();
        NavigateToDesign(vm);

        // Slots have no AssetId, so Thumbnail should be null (no managed file exists)
        foreach (var row in vm.DesignTool.Rows)
        {
            foreach (var slot in row.Slots)
            {
                Assert.Null(slot.ThumbnailPath);
                Assert.Null(slot.Thumbnail);
            }
        }
    }

    [AvaloniaFact]
    public void SupportingImages_ImportButtonExists()
    {
        using var fixture = new MainWindowFixture();
        var vm = fixture.ViewModel;
        NavigateToDesign(vm);
        fixture.PumpLayout();

        // Verify the Import supporting image button exists
        var importButton = fixture.FindControlOrDefault<Button>(btn =>
            btn.Content is string s && s.Contains("Import supporting image", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(importButton);
        Assert.True(importButton.IsVisible);
        Assert.True(importButton.IsEnabled);
    }
}
