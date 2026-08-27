using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
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
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Tests;

public class StoreEditorHeadlessTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public void ProductsTabButton_SelectsProductsTabAndShowsPanel()
    {
        var window = CreateEditorWindow();

        var button = FindButton(window, "Catalog & mockups");
        Assert.NotNull(button);

        button!.Command!.Execute(button.CommandParameter);
        window.UpdateLayout();
        window.UpdateLayout();

        var viewModel = (StoreManagementViewModel)window.DataContext!;
        Assert.True(viewModel.IsProductsTabSelected);

        var newProductButton = FindButton(window, "New Blueprint");
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

        var newProductButton = FindButton(window, "New Blueprint");
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
        Assert.NotNull(FindButton(window, "New Blueprint"));
        Assert.Null(FindButton(window, "Add Blueprint Offering"));

        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        window.UpdateLayout();
        Assert.True(viewModel.IsProductDetail);
        Assert.NotNull(FindButton(window, "Add Blueprint Offering"));
        Assert.Null(FindButton(window, "Add Provider-Network Offering"));
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<ComboBox>(),
            comboBox => IsEffectivelyVisible(comboBox) && comboBox.PlaceholderText == "Select Blueprint");
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<TextBox>(),
            textBox => IsEffectivelyVisible(textBox) && textBox.PlaceholderText?.StartsWith("Provider Network code", StringComparison.Ordinal) == true);
        var blueprintDetailText = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>()
            .Where(IsEffectivelyVisible)
            .Select(block => block.Text));
        Assert.DoesNotContain("Normalized catalog setup", blueprintDetailText, StringComparison.Ordinal);
        Assert.Null(FindButton(window, "Add Variant"));

        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();
        Assert.True(viewModel.IsOfferingDetail);
        Assert.NotNull(FindButton(window, "Manage Variants"));
        Assert.NotNull(FindButton(window, "Manage Design Areas"));
        Assert.NotNull(FindButton(window, "Manage Mockup Templates"));
        Assert.Null(FindButton(window, "Add Variant"));

        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        Assert.True(viewModel.IsVariantManagement);
        Assert.NotNull(FindButton(window, "Add Variant"));
        Assert.Null(FindButton(window, "Add Placeholder"));

        window.Close();
    }

    [AvaloniaFact]
    public void OfferingAndFocusedEditorsPreserveApprovedBroadComposition()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();

        AssertEffectivelyVisible(window, "Catalog.OfferingBasics");
        AssertEffectivelyVisible(window, "Catalog.OfferingSetup");
        AssertEffectivelyVisible(window, "Catalog.OfferingProvider");

        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        var available = AssertEffectivelyVisible(window, "Catalog.VariantAvailableChoices");
        var sellable = AssertEffectivelyVisible(window, "Catalog.VariantSellableVariants");
        Assert.True(available.Bounds.Top <= sellable.Bounds.Top,
            "Available choices should be presented before sellable variants.");
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<ToggleButton>(), toggle =>
            IsEffectivelyVisible(toggle) && string.Equals(toggle.Content as string, "Options & Values", StringComparison.Ordinal));
        var variantText = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>()
            .Where(IsEffectivelyVisible).Select(block => block.Text));
        Assert.Contains("Color", variantText, StringComparison.Ordinal);
        Assert.Contains("Size", variantText, StringComparison.Ordinal);
        Assert.NotNull(FindButton(window, "Manage values"));

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        window.UpdateLayout();
        AssertEffectivelyVisible(window, "Catalog.DesignAreaList");
        AssertEffectivelyVisible(window, "Catalog.DesignAreaEditor");

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        AssertEffectivelyVisible(window, "Catalog.MockupTemplateList");
        AssertEffectivelyVisible(window, "Catalog.MockupTemplateEditor");

        window.Close();
    }

    [AvaloniaFact]
    public void OfferingSetupRows_ShareAlignedCountAndActionColumns()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();

        var manageVariants = Assert.IsType<Button>(FindButton(window, "Manage Variants")!);
        var manageAreas = Assert.IsType<Button>(FindButton(window, "Manage Design Areas")!);
        var manageTemplates = Assert.IsType<Button>(FindButton(window, "Manage Mockup Templates")!);

        Assert.True(manageVariants.Bounds.Width > 0, "Manage buttons should be laid out.");
        Assert.Equal(manageVariants.Bounds.X, manageAreas.Bounds.X, 0.5);
        Assert.Equal(manageAreas.Bounds.X, manageTemplates.Bounds.X, 0.5);
        Assert.Equal(manageVariants.Bounds.Width, manageAreas.Bounds.Width, 0.5);
        Assert.Equal(manageAreas.Bounds.Width, manageTemplates.Bounds.Width, 0.5);

        var setup = AssertEffectivelyVisible(window, "Catalog.OfferingSetup");
        var countLabels = setup.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(IsEffectivelyVisible)
            .Where(block => block.Text?.EndsWith(" configured", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.Equal(3, countLabels.Length);
        Assert.Equal(countLabels[0].Bounds.Right, countLabels[1].Bounds.Right, 0.5);
        Assert.Equal(countLabels[1].Bounds.Right, countLabels[2].Bounds.Right, 0.5);

        window.Close();
    }

    [AvaloniaFact]
    public void AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var cards = window.GetVisualDescendants().OfType<Border>()
            .Where(border => AutomationProperties.GetAutomationId(border) == "Catalog.OptionCard")
            .ToArray();
        Assert.Equal(2, cards.Length);

        var kinds = cards.Select(card => ((OfferingChoiceGroupViewModel)card.DataContext!).Option.OptionKind).ToHashSet();
        Assert.Contains(OptionKind.Color, kinds);
        Assert.Contains(OptionKind.Size, kinds);

        foreach (var card in cards)
        {
            Assert.Equal(1, card.BorderThickness.Left);
            Assert.Equal(1, card.BorderThickness.Top);
            Assert.NotNull(card.BorderBrush);
            Assert.NotNull(card.Background);
            Assert.Equal(6, card.CornerRadius.TopLeft);
        }

        var color = cards.Single(card => ((OfferingChoiceGroupViewModel)card.DataContext!).Option.OptionKind == OptionKind.Color);
        var size = cards.Single(card => ((OfferingChoiceGroupViewModel)card.DataContext!).Option.OptionKind == OptionKind.Size);
        var colorTopLeft = color.TranslatePoint(new Avalonia.Point(0, 0), window);
        var sizeTopLeft = size.TranslatePoint(new Avalonia.Point(0, 0), window);
        Assert.NotNull(colorTopLeft);
        Assert.NotNull(sizeTopLeft);
        Assert.True(colorTopLeft.Value.Y == sizeTopLeft.Value.Y,
            "Cards should sit on one row at the default width.");
        Assert.True(colorTopLeft.Value.X + color.Bounds.Width <= sizeTopLeft.Value.X,
            $"Multiple cards should align on one row when the available width allows it. Color={colorTopLeft} Size={sizeTopLeft}");

        window.Width = window.MinWidth;
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        window.UpdateLayout();

        var narrowColor = color.TranslatePoint(new Avalonia.Point(0, 0), window);
        var narrowSize = size.TranslatePoint(new Avalonia.Point(0, 0), window);
        Assert.True(narrowColor!.Value.Y < narrowSize!.Value.Y,
            "Cards should wrap onto a new row when the window narrows.");
        Assert.Equal(narrowColor.Value.X, narrowSize.Value.X);

        window.Close();
    }

    [AvaloniaFact]
    public void BlueprintOfferingCard_ClickOpensOfferingOverview()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        window.UpdateLayout();

        var card = Assert.Single(viewModel.BlueprintOfferingCards);
        var offeringButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => ReferenceEquals(button.DataContext, card));

        Assert.NotNull(offeringButton.Command);
        Assert.Same(card, offeringButton.CommandParameter);
        offeringButton.Command.Execute(offeringButton.CommandParameter);
        window.UpdateLayout();

        Assert.True(viewModel.IsOfferingDetail);
        Assert.Equal(card.Id, viewModel.SelectedOffering?.Id);
        AssertEffectivelyVisible(window, "Catalog.OfferingStatus");

        window.Close();
    }

    [AvaloniaFact]
    public void BlueprintOfferingCard_ClickOpensOfferingAfterWorkspaceSwitch()
    {
        var window = CreateEditorWindowAfterWorkspaceSwitch();
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        window.UpdateLayout();

        var card = Assert.Single(viewModel.BlueprintOfferingCards);
        var offeringButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => ReferenceEquals(button.DataContext, card));

        Assert.NotNull(offeringButton.Command);
        Assert.Same(card, offeringButton.CommandParameter);
        offeringButton.Command.Execute(offeringButton.CommandParameter);
        window.UpdateLayout();

        Assert.True(viewModel.IsOfferingDetail);
        Assert.Equal(card.Id, viewModel.SelectedOffering?.Id);
        Assert.Equal(card.Id, viewModel.CatalogSetup!.SelectedOfferingId);
        Assert.False(viewModel.CatalogSetup.IsOfferingContextUnavailable);
        AssertEffectivelyVisible(window, "Catalog.OfferingStatus");

        window.Close();
    }

    [AvaloniaFact]
    public void CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        window.UpdateLayout();

        AssertEffectivelyVisible(window, "Catalog.BlueprintBasics");
        AssertEffectivelyVisible(window, "Catalog.BlueprintOfferingList");
        Assert.False(viewModel.IsBlueprintBasicsExpanded);
        Assert.Equal("Ready", Assert.Single(viewModel.BlueprintOfferingCards).Status);
        viewModel.IsBlueprintBasicsExpanded = true;
        window.UpdateLayout();
        Assert.True(viewModel.IsBlueprintBasicsExpanded);

        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();
        AssertEffectivelyVisible(window, "Catalog.OfferingStatus");

        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(),
            control => AutomationProperties.GetAutomationId(control) == "Catalog.OptionValueEditor");
        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        var manageValues = FindButton(window, "Manage values")!;
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var valueDialog = Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.Equal("Manage Color values", valueDialog.Title);
        Assert.True(valueDialog.FindControl<Button>("OptionValueDoneButton")!.IsFocused);
        valueDialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.False(viewModel.CatalogSetup!.IsManagingOptionValues);
        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.True(manageValues.IsFocused);
        Assert.Null(FindButton(window, "Preview valid Variants"));
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(),
            control => AutomationProperties.GetAutomationId(control) == "Catalog.BulkVariantEditor");
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(),
            control => AutomationProperties.GetAutomationId(control) == "Catalog.AddVariantEditor");
        FindButton(window, "Bulk add")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var bulkDialog = Assert.Single(window.OwnedWindows.OfType<BulkAddVariantsWindow>());
        Assert.Equal("Bulk add", bulkDialog.Title);
        Assert.True(bulkDialog.FindControl<ComboBox>("BulkColorComboBox")!.IsFocused);
        Assert.Null(FindButton(window, "Save Variant"));
        bulkDialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.False(viewModel.CatalogSetup!.IsAddingBulkVariants);
        Assert.Empty(window.OwnedWindows.OfType<BulkAddVariantsWindow>());
        Assert.True(window.FindControl<Button>("BulkAddVariantButton")!.IsFocused);

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        window.UpdateLayout();
        var designCard = Assert.Single(viewModel.CatalogSetup!.DesignAreaCards);
        Assert.Equal("All active Variants", designCard.CompatibilitySummary);
        Assert.Contains("px", designCard.MaximumSizeSummary, StringComparison.Ordinal);

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        var templateCard = Assert.Single(viewModel.CatalogSetup.MockupTemplateCards);
        Assert.Equal("Front", templateCard.TargetDesignArea);
        Assert.Equal(1, templateCard.CurrentRevision);
        viewModel.CatalogSetup.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        AssertEffectivelyVisible(window, "Catalog.MockupPreviewRegion");
        AssertEffectivelyVisible(window, "Catalog.MockupConfigurationRegion");

        window.Close();
    }

    [AvaloniaFact]
    public void CatalogStrategyControlShowsManualAndExplainsFutureIntegrations()
    {
        var window = CreateEditorWindow();
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        window.UpdateLayout();

        var strategy = window.GetVisualDescendants().OfType<ComboBox>()
            .Single(control => AutomationProperties.GetAutomationId(control) == "StoreEditor.FulfillmentStrategy");
        var text = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text));

        Assert.Contains(FulfillmentStrategy.Manual, strategy.ItemsSource as IEnumerable<FulfillmentStrategy> ?? []);
        Assert.Contains("Manual means", text, StringComparison.Ordinal);
        Assert.Contains("Shopify + Printify", text, StringComparison.Ordinal);

        window.Close();
    }

    [AvaloniaFact]
    public void CatalogDetailExposesFutureTemplateStateWithoutRenderingControls()
    {
        var window = CreateEditorWindow();
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();

        var text = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>().Where(IsEffectivelyVisible).Select(block => block.Text));
        Assert.Contains("Manage Mockup Templates", text, StringComparison.Ordinal);
        Assert.Equal(viewModel.SelectedOffering!.Id, viewModel.CatalogSetup!.SelectedOfferingId);
        Assert.Null(FindButton(window, "Add Option"));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        var addOption = FindButton(window, "Add Option");
        Assert.NotNull(addOption);
        Assert.Null(FindButton(window, "Add Option Value"));
        Assert.Null(FindButton(window, "Add Mockup Template"));
        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        Assert.NotNull(FindButton(window, "Add Mockup Template"));
        Assert.Null(FindButton(window, "Save Mockup Template"));
        Assert.Null(FindButton(window, "Link Color Option Value"));
        var mockupText = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>().Where(IsEffectivelyVisible).Select(block => block.Text));
        Assert.Contains("Provider mockup catalog data is not available", mockupText, StringComparison.Ordinal);
        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        addOption!.Command!.Execute(addOption.CommandParameter);
        window.UpdateLayout();
        Assert.NotNull(FindButton(window, "Save Option"));
        Assert.NotNull(FindButton(window, "Cancel"));
        var buttonLabels = string.Join(" ", window.GetVisualDescendants().OfType<Button>().Select(button => button.Content as string));
        Assert.DoesNotContain("Upload", buttonLabels, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("placement", buttonLabels, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("renderer", buttonLabels, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("override", buttonLabels, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<ComboBox>(),
            comboBox => IsEffectivelyVisible(comboBox) && comboBox.PlaceholderText == "Select Blueprint Offering");
        Assert.DoesNotContain("Placeholders", window.GetVisualDescendants().OfType<ToggleButton>()
            .Where(IsEffectivelyVisible).Select(toggle => toggle.Content as string));

        window.Close();
    }

    [AvaloniaFact]
    public void VariantsBackControl_ShowsChevronAndBindsOfferingOverviewCommand()
    {
        var window = CreateEditorWindow();
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var backButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(IsEffectivelyVisible)
            .Single(value => value.Content is string label &&
                label.Contains("Back to Offering overview", StringComparison.Ordinal));

        Assert.Equal("‹  Back to Offering overview", backButton.Content);
        Assert.Same(viewModel.BackToOfferingOverviewCommand, backButton.Command);

        backButton.Command!.Execute(null);
        window.UpdateLayout();
        Assert.True(viewModel.IsOfferingDetail);

        window.Close();
    }

    [AvaloniaFact]
    public void OfferingDetailWithoutNormalizedRecordRepairsAndShowsNormalizedEditor()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: false);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        window.UpdateLayout();

        var text = string.Join(" ", window.GetVisualDescendants().OfType<TextBlock>()
            .Where(IsEffectivelyVisible)
            .Select(block => block.Text));
        Assert.DoesNotContain("could not be repaired", text, StringComparison.Ordinal);
        Assert.NotNull(FindButton(window, "Manage Variants"));
        Assert.NotNull(FindButton(window, "Manage Mockup Templates"));
        Assert.Equal(viewModel.SelectedOffering!.Id, viewModel.CatalogSetup!.SelectedOfferingId);
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<ComboBox>(),
            comboBox => IsEffectivelyVisible(comboBox) && comboBox.PlaceholderText == "Select Blueprint Offering");

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
                    "Notes" &&
                textBox.Bounds.Width > 0)
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

    [AvaloniaFact]
    public void OptionCardsMoveArchiveIntoOverflowMenu()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        Assert.Null(FindButton(window, "Archive Option"));
        Assert.Null(FindButton(window, "Archive option"));
        Assert.NotNull(FindButton(window, "Manage values"));

        var overflowButtons = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(button => (AutomationProperties.GetAutomationId(button) ?? string.Empty).StartsWith("Catalog.OptionOverflow.", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, overflowButtons.Length);

        var accessibleNames = overflowButtons
            .Select(button => AutomationProperties.GetName(button))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "More actions for Color", "More actions for Size" }, accessibleNames);

        window.Close();
    }

    [AvaloniaFact]
    public void OptionOverflowMenu_OpensByPointerAndKeyboard()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var overflowButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "More actions for Color");
        var flyout = Assert.IsAssignableFrom<MenuFlyout>(overflowButton.Flyout);

        var center = overflowButton.TranslatePoint(new Point(overflowButton.Bounds.Width / 2, overflowButton.Bounds.Height / 2), window) ?? default;
        HeadlessWindowExtensions.MouseDown(window, center, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, center, MouseButton.Left, RawInputModifiers.None);
        window.UpdateLayout();
        Assert.True(flyout.IsOpen);
        flyout.Hide();

        overflowButton.Focus();
        HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
        window.UpdateLayout();
        Assert.True(flyout.IsOpen, "Enter on the focused overflow button should open the menu.");

        window.Close();
    }

    [AvaloniaFact]
    public void OptionOverflowMenu_ContainsDestructiveArchiveEntryForTheOption()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var overflowButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "More actions for Color");
        var flyout = Assert.IsAssignableFrom<MenuFlyout>(overflowButton.Flyout);

        flyout.ShowAt(overflowButton);
        window.UpdateLayout();
        var archiveItem = flyout.Items.OfType<MenuItem>()
            .Single(item => Equals(item.Header, "Archive option"));
        Assert.Contains("danger", archiveItem.Classes);
        Assert.NotNull(archiveItem.Command);
        Assert.IsAssignableFrom<OfferingOption>(archiveItem.CommandParameter);

        window.Close();
    }

    [AvaloniaFact]
    public void OptionOverflowMenu_InvokesArchiveAndSurfacesBlockedReason()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var overflowButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "More actions for Color");
        var flyout = Assert.IsAssignableFrom<MenuFlyout>(overflowButton.Flyout);
        flyout.ShowAt(overflowButton);
        window.UpdateLayout();
        var archiveItem = flyout.Items.OfType<MenuItem>()
            .Single(item => Equals(item.Header, "Archive option"));

        archiveItem.Command!.Execute(archiveItem.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.True(viewModel.CatalogSetup!.HasError);
        Assert.Contains("referenced", viewModel.CatalogSetup.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        window.Close();
    }

    [AvaloniaFact]
    public void OptionOverflowMenu_DismissalReturnsFocusAndMakesNoChange()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var overflowButton = window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "More actions for Color");
        overflowButton.Focus();
        window.UpdateLayout();
        Assert.True(overflowButton.IsFocused);

        var flyout = Assert.IsAssignableFrom<MenuFlyout>(overflowButton.Flyout);
        flyout.ShowAt(overflowButton);
        window.UpdateLayout();
        Assert.True(flyout.IsOpen);

        flyout.Hide();
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.False(flyout.IsOpen);
        Assert.True(overflowButton.IsFocused);
        Assert.NotNull(viewModel.CatalogSetup!.AvailableChoiceGroups);
        Assert.Equal(2, viewModel.CatalogSetup.AvailableChoiceGroups.Count);

        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_OpensFocusedDialogScopedToOneOption()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var manageValues = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(IsEffectivelyVisible)
            .Single(button => string.Equals(button.Content as string, "Manage values", StringComparison.Ordinal)
                && button.DataContext is OfferingChoiceGroupViewModel group
                && group.Option.OptionKind == OptionKind.Color);
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var dialog = Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.Equal("Manage Color values", dialog.Title);
        Assert.Equal(viewModel.CatalogSetup!.SelectedOptionId,
            ((OfferingChoiceGroupViewModel)manageValues.DataContext!).Option.Id);
        var doneButton = dialog.FindControl<Button>("OptionValueDoneButton")!;
        Assert.True(doneButton.IsFocused);
        Assert.NotNull(dialog.FindControl<Button>("AddOptionValueButton"));

        dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.False(viewModel.CatalogSetup.IsManagingOptionValues);
        Assert.True(manageValues.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_AllowsOnlyOneDialogAtATime()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var manageValues = FindButton(window, "Manage values")!;
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        var originalOptionId = viewModel.CatalogSetup!.SelectedOptionId;

        viewModel.CatalogSetup.ManageOptionCommand.Execute(
            viewModel.CatalogSetup.AvailableChoiceGroups.First(option => option.Option.OptionKind == OptionKind.Size).Option);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.Equal(originalOptionId, viewModel.CatalogSetup.SelectedOptionId);
        Assert.Equal("Manage Color values", Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>()).Title);
        foreach (var dialog in window.OwnedWindows.OfType<OptionValueManagementWindow>().ToArray()) dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_EscapeClosesDialogAndDiscardsAddValueDraft()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var manageValues = FindButton(window, "Manage values")!;
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var dialog = Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());

        viewModel.CatalogSetup!.StartAddOptionValueCommand.Execute(null);
        viewModel.CatalogSetup.OptionValue = "Navy";
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.True(viewModel.CatalogSetup.IsAddingOptionValue);

        HeadlessWindowExtensions.KeyPress(dialog, Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.False(viewModel.CatalogSetup.IsManagingOptionValues);
        Assert.False(viewModel.CatalogSetup.IsAddingOptionValue);
        Assert.Equal(string.Empty, viewModel.CatalogSetup.OptionValue);
        Assert.True(manageValues.IsFocused);
        Assert.Single(viewModel.CatalogSetup.AvailableChoiceGroups.Single(group => group.Option.OptionKind == OptionKind.Color).Values);

        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_CancelClosesDialogAndDiscardsAddValueDraft()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var manageValues = FindButton(window, "Manage values")!;
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        var dialog = Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());

        viewModel.CatalogSetup!.StartAddOptionValueCommand.Execute(null);
        viewModel.CatalogSetup.OptionValue = "Navy";
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        dialog.FindControl<Button>("OptionValueCancelButton")!.RaiseEvent(
            new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.False(viewModel.CatalogSetup.IsManagingOptionValues);
        Assert.False(viewModel.CatalogSetup.IsAddingOptionValue);
        Assert.Equal(string.Empty, viewModel.CatalogSetup.OptionValue);
        Assert.True(manageValues.IsFocused);
        Assert.Single(viewModel.CatalogSetup.AvailableChoiceGroups.Single(group => group.Option.OptionKind == OptionKind.Color).Values);

        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_OpensSameDialogForCustomOptionKind()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        viewModel.CatalogSetup!.SelectedOptionKind = OptionKind.Other;
        viewModel.CatalogSetup.OptionName = "Material";
        viewModel.CatalogSetup.StartAddOptionCommand.Execute(null);
        window.UpdateLayout();
        viewModel.CatalogSetup.CreateOptionCommand.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var customManageValues = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(IsEffectivelyVisible)
            .Single(button => string.Equals(button.Content as string, "Manage values", StringComparison.Ordinal)
                && button.DataContext is OfferingChoiceGroupViewModel group
                && group.Option.OptionKind == OptionKind.Other);
        customManageValues.Command!.Execute(customManageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var dialog = Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.Equal("Manage Material values", dialog.Title);

        dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.Close();
    }

    [AvaloniaFact]
    public void ManageValues_OfferingSwitchClosesDialogWithoutStaleEditing()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        var manageValues = FindButton(window, "Manage values")!;
        manageValues.Command!.Execute(manageValues.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Single(window.OwnedWindows.OfType<OptionValueManagementWindow>());

        viewModel.CatalogSetup!.SelectOffering(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Empty(window.OwnedWindows.OfType<OptionValueManagementWindow>());
        Assert.False(viewModel.CatalogSetup.IsManagingOptionValues);

        window.Close();
    }

    [AvaloniaFact]
    public void AddVariant_OpensFocusedDialogScopedToOffering()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        FindButton(window, "Add Variant")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var dialog = Assert.Single(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.Equal("Add Variant", dialog.Title);
        Assert.Equal(viewModel.CatalogSetup!.SelectedOfferingId, viewModel.SelectedOffering!.Id);
        Assert.True(dialog.FindControl<TextBox>("VariantNameTextBox")!.IsFocused);
        Assert.NotNull(dialog.FindControl<Button>("SaveVariantButton"));

        dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Empty(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.False(viewModel.CatalogSetup.IsAddingVariant);
        Assert.True(window.FindControl<Button>("AddVariantButton")!.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void BulkAdd_OpensFocusedDialogScopedToOffering()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        FindButton(window, "Bulk add")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var dialog = Assert.Single(window.OwnedWindows.OfType<BulkAddVariantsWindow>());
        Assert.Equal("Bulk add", dialog.Title);
        Assert.Equal(viewModel.CatalogSetup!.SelectedOfferingId, viewModel.SelectedOffering!.Id);
        Assert.True(dialog.FindControl<ComboBox>("BulkColorComboBox")!.IsFocused);
        Assert.NotNull(dialog.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == "Preview valid Variants"));

        dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Empty(window.OwnedWindows.OfType<BulkAddVariantsWindow>());
        Assert.False(viewModel.CatalogSetup.IsAddingBulkVariants);
        Assert.True(window.FindControl<Button>("BulkAddVariantButton")!.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void VariantCreation_AllowsOnlyOneDialogAtATime()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        FindButton(window, "Add Variant")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Single(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.Empty(window.OwnedWindows.OfType<BulkAddVariantsWindow>());

        Assert.Single(window.OwnedWindows.OfType<Window>());
        foreach (var dialog in window.OwnedWindows.OfType<Window>().ToArray()) dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Empty(window.OwnedWindows.OfType<Window>());

        FindButton(window, "Bulk add")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Single(window.OwnedWindows.OfType<BulkAddVariantsWindow>());
        Assert.Empty(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.Single(window.OwnedWindows.OfType<Window>());

        foreach (var dialog in window.OwnedWindows.OfType<Window>().ToArray()) dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        window.Close();
    }

    [AvaloniaFact]
    public void VariantCreation_EscapeClosesAndDiscardsDraft()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        FindButton(window, "Add Variant")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var dialog = Assert.Single(window.OwnedWindows.OfType<AddVariantWindow>());

        viewModel.CatalogSetup!.VariantName = "Draft";
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(viewModel.CatalogSetup.IsAddingVariant);

        HeadlessWindowExtensions.KeyPress(dialog, Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Empty(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.False(viewModel.CatalogSetup.IsAddingVariant);
        Assert.Equal(string.Empty, viewModel.CatalogSetup.VariantName);
        Assert.Equal(1, viewModel.CatalogSetup.SellableVariantRows.Count);
        Assert.True(window.FindControl<Button>("AddVariantButton")!.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void VariantCreation_OfferingSwitchClosesDialog()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        FindButton(window, "Add Variant")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Single(window.OwnedWindows.OfType<AddVariantWindow>());

        viewModel.CatalogSetup!.SelectOffering(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Empty(window.OwnedWindows.OfType<AddVariantWindow>());
        Assert.False(viewModel.CatalogSetup.IsAddingVariant);

        window.Close();
    }

    [AvaloniaFact]
    public void VariantCreation_SuccessClosesDialogAndRefreshesList()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();
        Assert.Equal(1, viewModel.CatalogSetup!.SellableVariantRows.Count);

        FindButton(window, "Add Variant")!.Command!.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var dialog = Assert.Single(window.OwnedWindows.OfType<AddVariantWindow>());

        var medium = viewModel.CatalogSetup.VariantValueChoices.Single(v => v.Value.Value == "M");
        medium.IsSelected = true;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(viewModel.CatalogSetup.CreateVariantCommand.CanExecute(null));
        Assert.True(dialog.IsVisible);

        viewModel.CatalogSetup.CreateVariantCommand.Execute(null);
        Assert.False(viewModel.CatalogSetup.HasError, $"Error: {viewModel.CatalogSetup.ErrorMessage}");
        Assert.False(viewModel.CatalogSetup.IsAddingVariant);
        Assert.Equal(2, viewModel.CatalogSetup.SellableVariantRows.Count);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        dialog.Close();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(viewModel.SelectedOffering!.Id, viewModel.CatalogSetup.SelectedOfferingId);

        window.Close();
    }

    [AvaloniaFact]
    public void ParentScreen_RendersNoInlineCreationEditor()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenVariantManagementCommand.Execute(null);
        window.UpdateLayout();

        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(),
            control => AutomationProperties.GetAutomationId(control) == "Catalog.BulkVariantEditor");
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Control>(),
            control => AutomationProperties.GetAutomationId(control) == "Catalog.AddVariantEditor");
        Assert.NotNull(FindButton(window, "Add Variant"));
        Assert.NotNull(FindButton(window, "Bulk add"));

        window.Close();
    }

    [AvaloniaFact]
    public void DesignAreaCard_ShowsEditArchiveHorizontal()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var editButton = Assert.IsType<Button>(FindButton(window, "Edit")!);
        var archiveButton = Assert.IsType<Button>(FindButton(window, "Archive")!);

        // Assert they share the same card (same listItem Border ancestor)
        var editCard = editButton.GetVisualAncestors().OfType<Border>()
            .Single(b => b.Classes.Contains("listItem"));
        var archiveCard = archiveButton.GetVisualAncestors().OfType<Border>()
            .Single(b => b.Classes.Contains("listItem"));
        Assert.Same(editCard, archiveCard);

        // Assert horizontal ordering: same Y, Archive to the right of Edit
        Assert.Equal(editButton.Bounds.Y, archiveButton.Bounds.Y, 0.5);
        Assert.True(archiveButton.Bounds.X > editButton.Bounds.X,
            "Archive button should be to the right of Edit button.");

        // Assert focus/tab order follows visual order (Edit then Archive)
        Assert.True(editButton.TabIndex <= archiveButton.TabIndex,
            "Edit should appear before Archive in tab order.");

        // Assert command bindings are non-null and unchanged
        Assert.NotNull(editButton.Command);
        Assert.NotNull(archiveButton.Command);

        window.Close();
    }

    [AvaloniaFact]
    public void DesignAreaSaveButton_UsesSentenceCaseLabel()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        window.UpdateLayout();

        viewModel.CatalogSetup!.StartAddPlaceholderCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var saveButton = FindButton(window, "Save design area");
        Assert.NotNull(saveButton);
        Assert.Null(FindButton(window, "Save Design Area"));
        Assert.Null(FindButton(window, "Save design"));
        Assert.NotNull(saveButton!.Command);

        window.Close();
    }

    [AvaloniaFact]
    public void SaveMockupTemplateButton_ShowsFullLabelWithoutClipping()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        viewModel.CatalogSetup.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var saveButton = FindButton(window, "Save Mockup Template");
        Assert.NotNull(saveButton);

        var textBlock = saveButton!.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "Save Mockup Template");
        Assert.NotNull(textBlock);
        Assert.True(textBlock!.Bounds.Width <= saveButton.Bounds.Width + 0.5,
            $"The label '{textBlock.Text}' (width {textBlock.Bounds.Width}) must fit inside the button (width {saveButton.Bounds.Width}).");

        Assert.True(saveButton.Bounds.Width > 104,
            $"Button width ({saveButton.Bounds.Width}) should exceed the old fixed 104px constraint.");
        Assert.Equal("Save Mockup Template", saveButton.Content as string);
        Assert.NotNull(FindButton(window, "Cancel"));

        window.Close();
    }

    [AvaloniaFact]
    public void MappingFields_HavePersistentLabelsAndAccessibleNames()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;

        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        viewModel.CatalogSetup.EditTemplateCommand.Execute(Assert.Single(viewModel.CatalogSetup.MockupTemplateCards));
        window.UpdateLayout();
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var configRegion = AssertEffectivelyVisible(window, "Catalog.MockupConfigurationRegion");

        // Find mapping TextBoxes by accessible name
        var mappingX = configRegion.GetVisualDescendants().OfType<TextBox>()
            .Single(tb => AutomationProperties.GetName(tb) == "X");
        var mappingY = configRegion.GetVisualDescendants().OfType<TextBox>()
            .Single(tb => AutomationProperties.GetName(tb) == "Y");
        var mappingWidth = configRegion.GetVisualDescendants().OfType<TextBox>()
            .Single(tb => AutomationProperties.GetName(tb) == "Width");
        var mappingHeight = configRegion.GetVisualDescendants().OfType<TextBox>()
            .Single(tb => AutomationProperties.GetName(tb) == "Height");

        // Fields contain values (non-empty) — identification does not rely on placeholders
        Assert.False(string.IsNullOrEmpty(mappingX.Text), "MappingX should have a value.");
        Assert.False(string.IsNullOrEmpty(mappingY.Text), "MappingY should have a value.");
        Assert.False(string.IsNullOrEmpty(mappingWidth.Text), "MappingWidth should have a value.");
        Assert.False(string.IsNullOrEmpty(mappingHeight.Text), "MappingHeight should have a value.");

        // Persistent visible labels identify each field
        var labelTexts = configRegion.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(IsEffectivelyVisible)
            .Select(tb => tb.Text)
            .ToHashSet();
        Assert.Contains("X", labelTexts);
        Assert.Contains("Y", labelTexts);
        Assert.Contains("Width", labelTexts);
        Assert.Contains("Height", labelTexts);

        // Tab order: X before Y before Width before Height
        Assert.True(mappingX.Bounds.Left < mappingY.Bounds.Left,
            "X should be left of Y.");
        Assert.True(mappingY.Bounds.Top <= mappingWidth.Bounds.Top,
            "Y should be above or level with Width.");
        Assert.True(mappingWidth.Bounds.Left < mappingHeight.Bounds.Left,
            "Width should be left of Height.");

        window.Close();
    }

    [AvaloniaFact]
    public void MockupPreview_WithoutImageShowsCompactUnavailableStateAndNoRectangle()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        viewModel.CatalogSetup!.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.False(viewModel.CatalogSetup.HasSelectedProviderMockup);
        Assert.False(viewModel.CatalogSetup.HasProviderMockupCandidates);

        var editor = window.GetVisualDescendants().OfType<MockupPlacementEditor>().Single();
        Assert.False(IsEffectivelyVisible(editor));

        var previewRegion = AssertEffectivelyVisible(window, "Catalog.MockupPreviewRegion");
        var unavailable = previewRegion.GetVisualDescendants().OfType<TextBlock>()
            .Where(IsEffectivelyVisible)
            .FirstOrDefault(block => !string.IsNullOrEmpty(block.Text)
                && block.Text.Contains("available", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(unavailable);

        window.Close();
    }

    [AvaloniaFact]
    public void MockupPreview_WithImageSynchronizesPlacementRectangleAndMappingFields()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        viewModel.CatalogSetup!.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var candidate = new ProviderMockupCandidateDescriptor("front-black", "Front preview", 1000, 1000, new HashSet<Guid>());
        viewModel.CatalogSetup.ProviderMockupCandidates.Add(candidate);
        viewModel.CatalogSetup.SelectedProviderMockup = candidate;
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var editor = window.GetVisualDescendants().OfType<MockupPlacementEditor>().Single();
        Assert.True(IsEffectivelyVisible(editor));
        Assert.Equal(viewModel.CatalogSetup.MappingX, editor.PlacementX, 1);
        Assert.Equal(viewModel.CatalogSetup.MappingY, editor.PlacementY, 1);
        Assert.Equal(viewModel.CatalogSetup.MappingWidth, editor.PlacementWidth, 1);
        Assert.Equal(viewModel.CatalogSetup.MappingHeight, editor.PlacementHeight, 1);

        editor.PlacementX = 333;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(333, viewModel.CatalogSetup.MappingX, 1);
        Assert.True(viewModel.CatalogSetup.MappingX + viewModel.CatalogSetup.MappingWidth <= viewModel.CatalogSetup.MappingImageWidth);

        viewModel.CatalogSetup.MappingY = 412;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(412, editor.PlacementY, 1);

        window.Close();
    }

    [AvaloniaFact]
    public void AdvancedProviderDataExpander_ShowsPopulatedState()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var templateCard = Assert.Single(viewModel.CatalogSetup!.MockupTemplateCards);
        viewModel.CatalogSetup.EditTemplateCommand.Execute(templateCard);
        window.UpdateLayout();
        window.UpdateLayout();

        var reference = "front-black";
        var candidate = new ProviderMockupCandidateDescriptor(reference, "Front preview", 1200, 1200, new HashSet<Guid>());
        viewModel.CatalogSetup.ProviderMockupCandidates.Add(candidate);
        viewModel.CatalogSetup.SelectedProviderMockup = candidate;
        window.UpdateLayout();
        window.UpdateLayout();

        var expander = window.GetVisualDescendants().OfType<Expander>()
            .Single(e => string.Equals(e.Header as string, "Advanced provider data", StringComparison.Ordinal) && IsEffectivelyVisible(e));
        expander.IsExpanded = true;
        window.UpdateLayout();
        window.UpdateLayout();

        var label = window.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(t => IsEffectivelyVisible(t) && string.Equals(t.Text, "Provider mockup reference", StringComparison.Ordinal));
        Assert.NotNull(label);
        Assert.True(label!.IsVisible);

        var valueBox = window.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(t => IsEffectivelyVisible(t) && t.IsReadOnly && string.Equals(t.Text, reference, StringComparison.Ordinal));
        Assert.NotNull(valueBox);
        Assert.True(valueBox!.IsReadOnly);

        window.Close();
    }

    [AvaloniaFact]
    public void AdvancedProviderDataExpander_ShowsUnavailableState()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        viewModel.CatalogSetup!.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var expander = window.GetVisualDescendants().OfType<Expander>()
            .Single(e => string.Equals(e.Header as string, "Advanced provider data", StringComparison.Ordinal) && IsEffectivelyVisible(e));
        expander.IsExpanded = true;
        window.UpdateLayout();
        window.UpdateLayout();

        var unavailableText = window.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(t => IsEffectivelyVisible(t) && string.Equals(t.Text, "No provider reference available.", StringComparison.Ordinal));
        Assert.NotNull(unavailableText);

        var anyValueControl = window.GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(t => IsEffectivelyVisible(t) && t.IsReadOnly && !string.IsNullOrEmpty(t.Text));
        Assert.Null(anyValueControl);

        window.Close();
    }

    [AvaloniaFact]
    public void AdvancedProviderDataExpander_ExposesExpandedState()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        viewModel.CatalogSetup!.StartAddTemplateCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var expander = window.GetVisualDescendants().OfType<Expander>()
            .Single(e => string.Equals(e.Header as string, "Advanced provider data", StringComparison.Ordinal) && IsEffectivelyVisible(e));

        Assert.NotNull(expander);
        Assert.False(expander.IsExpanded);

        expander.IsExpanded = true;
        window.UpdateLayout();
        window.UpdateLayout();
        Assert.True(expander.IsExpanded);

        expander.IsExpanded = false;
        window.UpdateLayout();
        window.UpdateLayout();
        Assert.False(expander.IsExpanded);

        window.Close();
    }

    [AvaloniaFact]
    public void DesignAreaArchiveCommand_OpensConfirmationWithoutMutationAndCancelRestoresFocus()
    {
        var window = CreateEditorWindow(includeNormalizedCatalog: true, useFixedProviderOffering: true, includeOfferingOptions: true);
        var viewModel = (StoreManagementViewModel)window.DataContext!;
        viewModel.SelectProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings));
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        window.UpdateLayout();
        window.UpdateLayout();

        var archiveButton = FindButton(window, "Archive");
        Assert.NotNull(archiveButton);
        archiveButton!.Focus();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(archiveButton.IsFocused);

        archiveButton.Command!.Execute(archiveButton.CommandParameter);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.True(viewModel.CatalogSetup!.IsDesignAreaArchiveConfirmationVisible);
        Assert.Equal("Front", viewModel.CatalogSetup.PendingDesignAreaArchiveName);
        Assert.Single(viewModel.CatalogSetup.DesignAreaCards);
        Assert.False(viewModel.CatalogSetup.HasError);

        var dialog = Assert.Single(window.OwnedWindows.OfType<DesignAreaArchiveConfirmationWindow>());
        var message = dialog.GetVisualDescendants().OfType<TextBlock>()
            .Single(t => t.Text?.Contains("Front", StringComparison.Ordinal) == true);
        Assert.Contains("Front", message.Text, StringComparison.Ordinal);
        Assert.NotNull(dialog.FindControl<Button>("CancelButton"));
        Assert.NotNull(dialog.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == "Archive design area"));

        dialog.Close(false);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.False(viewModel.CatalogSetup.IsDesignAreaArchiveConfirmationVisible);
        Assert.Single(viewModel.CatalogSetup.DesignAreaCards);
        Assert.False(viewModel.CatalogSetup.HasError);
        Assert.True(archiveButton.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void DesignAreaArchiveConfirmationDialog_ShowsTargetNameFocusesCancelAndDismissesOnEscape()
    {
        var catalog = CreateStandaloneCatalogWithPendingDesignAreaArchive();
        Assert.True(catalog.IsDesignAreaArchiveConfirmationVisible);
        Assert.Equal("Front", catalog.PendingDesignAreaArchiveName);

        var dialog = new DesignAreaArchiveConfirmationWindow { DataContext = catalog };
        try
        {
            dialog.Show();
            dialog.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();

            var message = dialog.GetVisualDescendants().OfType<TextBlock>()
                .Single(t => t.Text?.Contains("Front", StringComparison.Ordinal) == true);
            Assert.Contains("Front", message.Text, StringComparison.Ordinal);
            Assert.Contains("leave the active Design Area list", message.Text, StringComparison.Ordinal);

            var cancelButton = dialog.FindControl<Button>("CancelButton")!;
            var confirmButton = dialog.GetVisualDescendants().OfType<Button>()
                .Single(b => (b.Content as string) == "Archive design area");
            Assert.Equal("Cancel archive", AutomationProperties.GetName(cancelButton));
            Assert.Equal("Confirm archive design area", AutomationProperties.GetName(confirmButton));
            Assert.True(cancelButton.IsFocused);

            HeadlessWindowExtensions.KeyPress(dialog, Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.False(dialog.IsVisible);
        }
        finally
        {
            if (dialog.IsVisible) dialog.Close();
        }
    }

    private static CatalogSetupViewModel CreateStandaloneCatalogWithPendingDesignAreaArchive()
    {
        var store = new Store(Guid.NewGuid(), "North Star", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(Snapshot(store, true, true, true));
        var catalog = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository));
        catalog.LoadForStoreAsync(store.Id, default).GetAwaiter().GetResult();
        var offering = Assert.Single(catalog.Offerings);
        catalog.SelectOffering(offering.Id);
        catalog.ArchivePlaceholderCommand.Execute(Assert.Single(catalog.DesignAreaCards));
        return catalog;
    }

    private static StoreEditorWindow CreateEditorWindow(
        bool includeNormalizedCatalog = true,
        bool useFixedProviderOffering = false,
        bool includeOfferingOptions = false)
    {
        var store = new Store(Guid.NewGuid(), "North Star", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(Snapshot(store, includeNormalizedCatalog, useFixedProviderOffering, includeOfferingOptions));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new ProductSupplierSetupService(repository),
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            new OfferingManagementService(repository));
        viewModel.LoadAsync(default).GetAwaiter().GetResult();
        var window = new StoreEditorWindow { DataContext = viewModel };
        window.Show();
        window.UpdateLayout();
        window.UpdateLayout();
        return window;
    }

    private static StoreEditorWindow CreateEditorWindowAfterWorkspaceSwitch()
    {
        // The second workspace's catalog is normalized-only: the Blueprint Offering
        // has no legacy FulfillmentOffering mirror until catalog synchronization repairs it.
        var workspaceA = WorkspaceSnapshot.DefaultWorkspace(Now);
        var workspaceB = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Seasonal", null, false, Now, Now, "{}");
        var storeA = new Store(Guid.NewGuid(), workspaceA.Id, "Alpha Tees", null, false, Now, Now, "{}");
        var storeB = new Store(Guid.NewGuid(), workspaceB.Id, "SwiftPod Store", null, false, Now, Now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), storeB.Id, "Gildan 64000 t-shirt", null, null, Now, Now, "{}");
        var blueprint = new Blueprint(product.Id, storeB.Id, product.Name, product.Description, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), storeB.Id, "SwiftPOD", null, false, Now, Now);
        var normalizedOffering = new BlueprintOffering(
            Guid.NewGuid(), blueprint.Id, storeB.Id, "Gildan 64000", null,
            BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, null, false, Now, Now);
        var snapshot = new WorkspaceSnapshot(
            [workspaceA, workspaceB],
            [storeA, storeB],
            [], [], [], [], [], [], [], [])
        {
            StoreProducts = [product],
            Blueprints = [blueprint],
            PrintProviders = [provider],
            BlueprintOfferings = [normalizedOffering]
        };
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new ProductSupplierSetupService(repository),
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            new OfferingManagementService(repository));
        viewModel.LoadAsync(default).GetAwaiter().GetResult();
        viewModel.SetActiveWorkspaceAsync(workspaceB.Id).GetAwaiter().GetResult();
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
                (string.Equals(b.Content as string, content, System.StringComparison.Ordinal) ||
                 b.GetVisualDescendants().OfType<TextBlock>().Any(text =>
                     string.Equals(text.Text, content, System.StringComparison.Ordinal))));

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

    private static Control AssertEffectivelyVisible(Window window, string automationId)
    {
        var control = window.GetVisualDescendants().OfType<Control>()
            .Single(value => AutomationProperties.GetAutomationId(value) == automationId);
        Assert.True(IsEffectivelyVisible(control), $"{automationId} should be visible.");
        return control;
    }

    private static WorkspaceSnapshot Snapshot(
        Store store,
        bool includeNormalizedCatalog,
        bool useFixedProviderOffering,
        bool includeOfferingOptions)
    {
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Gildan 64000", null, null, Now, Now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot(
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

        if (!includeNormalizedCatalog)
        {
            return snapshot;
        }

        var blueprint = new Blueprint(product.Id, store.Id, product.Name, product.Description, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "Printful", null, false, Now, Now);
        var normalizedOffering = new BlueprintOffering(
            offering.Id, blueprint.Id, store.Id, offering.Name, offering.Description,
            useFixedProviderOffering ? BlueprintOfferingKind.FixedPrintProvider : BlueprintOfferingKind.ProviderNetwork,
            useFixedProviderOffering ? provider.Id : null,
            useFixedProviderOffering ? null : "printful",
            null, offering.ExternalOfferingId, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var small = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "S", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 1);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / S", [black.Id, small.Id], false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 4500, 5400, [variant.Id], false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front black", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now, providerMockupReference: "front-black", imageMapping: new MockupImageSpaceMapping(1200, 1200, 250, 200, 600, 700));
        var templateColor = new MockupTemplateColorVariant(Guid.NewGuid(), template.Id, black.Id, false, Now, Now);
        return snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [normalizedOffering],
            PrintProviders = useFixedProviderOffering ? [provider] : [],
            OfferingOptions = includeOfferingOptions ? [colorOption, sizeOption] : [],
            OfferingOptionValues = includeOfferingOptions ? [black, small, medium] : [],
            OfferingVariants = includeOfferingOptions ? [variant] : [],
            OfferingPlaceholders = includeOfferingOptions ? [area] : [],
            MockupTemplates = includeOfferingOptions ? [template] : [],
            MockupTemplateColorVariants = includeOfferingOptions ? [templateColor] : [],
            MockupTemplateRevisions = includeOfferingOptions ? [revision] : []
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
