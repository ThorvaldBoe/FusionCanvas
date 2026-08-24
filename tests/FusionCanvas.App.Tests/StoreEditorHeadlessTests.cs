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
        var optionEditor = window.GetVisualDescendants().OfType<Control>()
            .Single(value => AutomationProperties.GetAutomationId(value) == "Catalog.OptionValueEditor");
        Assert.False(IsEffectivelyVisible(optionEditor));
        FindButton(window, "Manage values")!.Command!.Execute(FindButton(window, "Manage values")!.CommandParameter);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(IsEffectivelyVisible(optionEditor));
        Assert.True(window.FindControl<Button>("OptionValueDoneButton")!.IsFocused);
        Assert.Null(FindButton(window, "Preview valid Variants"));
        FindButton(window, "Bulk add")!.Command!.Execute(null);
        window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        AssertEffectivelyVisible(window, "Catalog.BulkVariantEditor");
        Assert.True(window.FindControl<ComboBox>("BulkColorComboBox")!.IsFocused);
        Assert.Null(FindButton(window, "Save Variant"));
        viewModel.CatalogSetup.CancelBulkVariantsCommand.Execute(null);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
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
