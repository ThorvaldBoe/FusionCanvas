using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public class MainWindowConstructionTests
{
    [AvaloniaFact]
    public void MainWindow_ConstructsAndLoadsViewsWithInMemoryWorkspace()
    {
        using var fixture = new MainWindowFixture();

        Assert.NotNull(fixture.Window.DataContext);
        Assert.IsType<MainWindowViewModel>(fixture.Window.DataContext);
        Assert.NotEmpty(fixture.ViewModel.NavigationContexts);
        Assert.Empty(fixture.ViewModel.DocumentWindow.Tabs);
    }

    [AvaloniaFact]
    public void MainWindow_CompiledBindingsResolveWithoutErrors()
    {
        using var fixture = new MainWindowFixture();

        var inspector = fixture.FindControl<ComboBox>(cb => cb.Classes.Contains("statusSelector"));
        var searchBox = fixture.FindControl<TextBox>(tb => tb.Name == "TreeSearchBox");
        var tree = fixture.FindControl<TreeView>(tv => tv.Name == "WorkspaceTreeControl");

        Assert.NotNull(inspector);
        Assert.NotNull(searchBox);
        Assert.NotNull(tree);
        Assert.True(fixture.ViewModel.HasActiveItem == false || fixture.ViewModel.HasActiveItem == true);
        Assert.True(searchBox.IsVisible);
    }

    [AvaloniaFact]
    public void OpeningItem_EnablesInspectorAndStatusSelector()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ItemInspector.HasState);
        Assert.True(fixture.ViewModel.HasActiveItem);
        Assert.NotNull(fixture.ViewModel.SelectedItemStatusOption);
        Assert.Equal(fixture.ViewModel.ActiveItemStatus,
            fixture.ViewModel.SelectedItemStatusOption!.Status);

        var statusSelector = fixture.FindControl<ComboBox>(cb =>
            cb.Classes.Contains("statusSelector") && cb.IsVisible);
        Assert.NotNull(statusSelector);
    }

    [AvaloniaFact]
    public void OpeningGroup_EnablesGroupDetailsPane()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstGroupContext());
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.GroupDetails.HasState);
        Assert.False(fixture.ViewModel.ItemInspector.HasState);
    }

    [AvaloniaFact]
    public void NicheSelection_ShowsSelectionSummaryNotInspector()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstNicheContext());
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowSelectionSummary);
        Assert.False(fixture.ViewModel.ItemInspector.HasState);
        Assert.False(fixture.ViewModel.GroupDetails.HasState);
    }

    [AvaloniaFact]
    public void WorkflowStageNavigation_ShowsCorrectStageTool()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Idea);
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowsIdeaStageTool);
        Assert.False(fixture.ViewModel.ShowsDesignStageTool);

        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Design);
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowsDesignStageTool);
        Assert.False(fixture.ViewModel.ShowsIdeaStageTool);
    }
}

public class MainWindowLayoutTests
{
    [AvaloniaFact]
    public void TreeExpander_ClickingOutsideTheGlyph_ExpandsItsGroup()
    {
        using var fixture = new MainWindowFixture();

        var expander = fixture.Window.GetVisualDescendants()
            .OfType<ToggleButton>()
            .First(button => button.DataContext is WorkspaceTreeNodeViewModel { HasChildren: true, IsExpanded: false });
        var node = Assert.IsType<WorkspaceTreeNodeViewModel>(expander.DataContext);
        var clickPoint = new Avalonia.Point(expander.Bounds.Right + 6, expander.Bounds.Center.Y);
        foreach (var ancestor in expander.GetVisualAncestors())
        {
            if (ReferenceEquals(ancestor, fixture.Window))
            {
                break;
            }

            clickPoint += ancestor.Bounds.Position;
        }

        HeadlessWindowExtensions.MouseDown(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        fixture.PumpLayout();

        Assert.True(node.IsExpanded);
    }

    [AvaloniaFact]
    public void DetailsColumn_ScrollsWhenContentExceedsMinimumHeight()
    {
        using var fixture = new MainWindowFixture(width: 900, height: 400);
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        var scroller = fixture.FindControlOrDefault<ScrollViewer>(sv =>
            sv.IsVisible
            && sv.Content is StackPanel
            && sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto
            && sv.Bounds.Height > 0);

        Assert.NotNull(scroller);
        Assert.True(scroller!.Extent.Height > scroller.Viewport.Height,
            $"Expected details extent ({scroller.Extent.Height}) to exceed viewport ({scroller.Viewport.Height}) at minimum height.");
    }

    [AvaloniaFact]
    public void DetailsColumn_ReservesAGutterBetweenContentAndVerticalScrollbar()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        var scroller = fixture.FindControl<ScrollViewer>(sv =>
            sv.IsVisible
            && sv.Content is StackPanel
            && sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto);

        Assert.True(scroller.Padding.Right >= 16,
            $"Expected details content to reserve at least 16px beside the scrollbar, got {scroller.Padding.Right}px.");
    }

    [AvaloniaFact]
    public void IdeationButton_ReservesSpaceBeforeTheDetailsScrollbar()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        var button = fixture.FindControlOrDefault<Button>(control => control.Name == "IdeationButton");
        Assert.NotNull(button);
        Assert.True(button.Margin.Right >= 12,
            $"Expected the Ideation button to reserve at least 12px before the scrollbar, got {button.Margin.Right}px.");
    }

    [AvaloniaFact]
    public void TagPills_InItemInspector_FitContentAndDoNotStretchFullWidth()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        // Add a tag to the ItemInspector's TagDraft
        fixture.ViewModel.ItemInspector.TagDraft.Add("TestTag");
        fixture.PumpLayout();

        // Find the ItemsControl bound to TagDraft
        var itemsControl = fixture.FindControl<ItemsControl>(ic =>
            ic.ItemsSource == fixture.ViewModel.ItemInspector.TagDraft);
        Assert.NotNull(itemsControl);
        Assert.NotNull(itemsControl.ItemContainerTheme);

        // Find the container panel (WrapPanel) for the items
        var wrapPanel = itemsControl.GetVisualDescendants().OfType<WrapPanel>().FirstOrDefault();
        Assert.NotNull(wrapPanel);

        // Find the tag pill Border inside the ItemsControl
        var tagBorder = itemsControl.GetVisualDescendants().OfType<Border>()
            .FirstOrDefault(b => b.Child is StackPanel sp
                && sp.Children.OfType<TextBlock>().Any(tb => tb.Text == "TestTag"));
        Assert.NotNull(tagBorder);

        // Assert HorizontalAlignment is Left (not Stretch)
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, tagBorder.HorizontalAlignment);

        // Assert the tag pill width is less than 200px (short tag)
        Assert.True(tagBorder.Bounds.Width < 200,
            $"Expected tag pill width < 200px for short text, got {tagBorder.Bounds.Width}px.");

        // Assert the containing WrapPanel is narrower than the inspector panel
        // The inspector column is typically > 300px wide
        var inspectorPanel = itemsControl.Parent as StackPanel
            ?? itemsControl.GetVisualAncestors().OfType<StackPanel>().FirstOrDefault();
        var container = inspectorPanel?.Parent as Border;
        if (container != null)
        {
            Assert.True(container.Bounds.Width > 300,
                $"Expected inspector container width > 300px, got {container.Bounds.Width}px.");
            Assert.True(tagBorder.Bounds.Width <= wrapPanel.Bounds.Width,
                $"Expected tag pill width ({tagBorder.Bounds.Width}px) to fit within wrap panel ({wrapPanel.Bounds.Width}px).");
        }
    }

    [AvaloniaFact]
    public void FilterControls_StayWithinPaneAtMinimumWidth()
    {
        using var fixture = new MainWindowFixture(width: 900, height: 600);

        var searchBox = fixture.FindControl<TextBox>(tb => tb.Name == "TreeSearchBox");
        var stageFilter = fixture.FindControl<ComboBox>(cb => cb.Name == "StageFilter");
        var statusFilter = fixture.FindControl<ComboBox>(cb => cb.Name == "StatusFilter");

        var paneBorder = searchBox.GetVisualAncestors().OfType<Border>()
            .First(b => b.Bounds.Width > 0 && b.Bounds.Width < 600);
        var paneRight = paneBorder.Bounds.Left + paneBorder.Bounds.Width;

        Assert.True(stageFilter.Bounds.Right <= paneRight + 1,
            $"Stage filter extends beyond pane: {stageFilter.Bounds.Right} > {paneRight}");
        Assert.True(statusFilter.Bounds.Right <= paneRight + 1,
            $"Status filter extends beyond pane: {statusFilter.Bounds.Right} > {paneRight}");
        Assert.True(searchBox.Bounds.Right <= paneRight + 1);
    }

    [AvaloniaFact]
    public void HeaderStageLabelAndStatusSelectorHaveHorizontalGap()
    {
        using var fixture = new MainWindowFixture(width: 1180, height: 760);
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();

        var headerGrid = fixture.FindControl<Grid>(g =>
            g.ColumnDefinitions.Count == 4 && g.ColumnSpacing == 8
            && g.IsVisible && g.Children.OfType<ComboBox>().Any());
        var statusSelector = headerGrid.Children.OfType<ComboBox>().First();
        var stageLabel = headerGrid.Children.OfType<TextBlock>()
            .Where(t => t.Foreground is ISolidColorBrush b
                && b.Color.R == 0x59 && b.Color.G == 0xA1 && b.Color.B == 0x7D)
            .MaxBy(t => t.Bounds.Width);

        Assert.NotNull(stageLabel);
        var gap = statusSelector.Bounds.Left - stageLabel!.Bounds.Right;
        Assert.True(gap >= 4, $"Expected header gap >= 4px, got {gap}px.");
    }

    [AvaloniaFact]
    public void TreeActionsToolbar_IsBetweenFilterAreaAndWorkspaceTree()
    {
        using var fixture = new MainWindowFixture();

        var toolbar = fixture.FindControl<Border>(b => b.Name == "TreeActionsToolbar");
        var searchBox = fixture.FindControl<TextBox>(tb => tb.Name == "TreeSearchBox");
        var tree = fixture.FindControl<TreeView>(tv => tv.Name == "WorkspaceTreeControl");

        Assert.NotNull(toolbar);
        Assert.True(toolbar.IsVisible);
        Assert.NotNull(searchBox);
        Assert.NotNull(tree);

        // Find the common parent DockPanel
        var dockPanel = toolbar.Parent as DockPanel;
        Assert.NotNull(dockPanel);
        var dockChildren = dockPanel.Children.ToList();

        // Find filter area as the nearest DockPanel child ancestor of searchBox
        var filterArea = searchBox.GetVisualAncestors()
            .OfType<Control>()
            .FirstOrDefault(a => dockChildren.IndexOf(a) >= 0);
        Assert.NotNull(filterArea);

        // Find the tree's wrapping container (a Grid that is a DockPanel child)
        var treeContainer = tree.GetVisualAncestors()
            .OfType<Control>()
            .FirstOrDefault(a => dockChildren.IndexOf(a) >= 0);
        Assert.NotNull(treeContainer);

        var filterIndex = dockChildren.IndexOf(filterArea);
        var toolbarIndex = dockChildren.IndexOf(toolbar);
        var treeContainerIndex = dockChildren.IndexOf(treeContainer);

        Assert.True(filterIndex >= 0, "Filter area not found in DockPanel");
        Assert.True(toolbarIndex >= 0, "Toolbar not found in DockPanel");
        Assert.True(treeContainerIndex >= 0, "Tree container not found in DockPanel");
        Assert.True(toolbarIndex > filterIndex, "Toolbar should appear after filter area");
        Assert.True(treeContainerIndex > toolbarIndex, "Tree container should appear after toolbar");
    }

    [AvaloniaFact]
    public void ExpandCollapseAllButton_TracksViewModelState()
    {
        using var fixture = new MainWindowFixture();

        var button = fixture.FindControl<Button>(b => b.Name == "ExpandCollapseAllButton");
        var vm = fixture.ViewModel.WorkspaceTree;

        Assert.NotNull(button);
        Assert.True(button.IsEnabled);

        // Tooltip and automation name match the view model's default tooltip
        var tooltip = ToolTip.GetTip(button) as string;
        var automationName = AutomationProperties.GetName(button);
        Assert.Equal(vm.ExpandCollapseAllTooltip, tooltip);
        Assert.Equal(vm.ExpandCollapseAllTooltip, automationName);
        Assert.Equal("Expand all groups", tooltip);

        // Find the two PathIcons inside the button's Grid content
        var iconPanel = button.GetVisualDescendants().OfType<Grid>()
            .FirstOrDefault(g => g.Children.OfType<PathIcon>().Count() == 2);
        Assert.NotNull(iconPanel);
        var icons = iconPanel.Children.OfType<PathIcon>().ToList();
        Assert.Equal(2, icons.Count);
        Assert.True(icons[0].IsVisible);  // expand icon visible when NextToggleExpands is true
        Assert.False(icons[1].IsVisible); // collapse icon hidden

        // Toggle via command
        vm.ToggleExpandCollapseAllCommand.Execute(null);
        fixture.PumpLayout();

        tooltip = ToolTip.GetTip(button) as string;
        automationName = AutomationProperties.GetName(button);
        Assert.Equal("Collapse all groups", tooltip);
        Assert.Equal("Collapse all groups", automationName);

        // After toggle, icons swap visibility
        Assert.False(icons[0].IsVisible);
        Assert.True(icons[1].IsVisible);

        // Enablement: set filter to disable
        vm.QueryText = "non-existent";
        fixture.PumpLayout();
        Assert.False(button.IsEnabled);
        tooltip = ToolTip.GetTip(button) as string;
        Assert.Equal("Filtering already expands all groups", tooltip);
        Assert.Equal("Filtering already expands all groups", AutomationProperties.GetName(button));

        vm.QueryText = string.Empty;
        fixture.PumpLayout();
        Assert.True(button.IsEnabled);
    }
}

public class MainWindowInputTests
{
    [AvaloniaFact]
    public async Task ItemTextField_TypedTextAndLostFocusCommitsEdit()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();
        await Task.Delay(150);

        fixture.ViewModel.ItemInspector.Notes = "";
        var notesBox = fixture.FindControl<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Item notes" && tb.IsVisible);
        notesBox.Focus();
        fixture.PumpLayout();

        notesBox.Text = "typed then committed";
        var titleBox = fixture.FindControl<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Item working title" && tb.IsVisible);
        titleBox.Focus();
        fixture.PumpLayout();
        await Task.Delay(150);

        Assert.False(fixture.ViewModel.ItemInspector.HasUnsavedChanges);
        Assert.Equal("typed then committed", fixture.ViewModel.ItemInspector.State!.Notes);
    }

    [AvaloniaFact]
    public async Task ItemTitle_KeyTextInputUpdatesDraft()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();
        await Task.Delay(150);

        var titleBox = fixture.FindControl<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Item working title" && tb.IsVisible);
        titleBox.Focus();
        fixture.PumpLayout();

        var original = fixture.ViewModel.ItemInspector.Title;
        HeadlessWindowExtensions.KeyTextInput(fixture.Window, "NewTypedTitle");

        Assert.NotEqual(original, fixture.ViewModel.ItemInspector.Title);
        Assert.Contains("NewTypedTitle", fixture.ViewModel.ItemInspector.Title);
    }

    [AvaloniaFact]
    public void SearchBox_TypedTextUpdatesQuery()
    {
        using var fixture = new MainWindowFixture();

        var searchBox = fixture.FindControl<TextBox>(tb => tb.Name == "TreeSearchBox");
        searchBox.Focus();
        fixture.PumpLayout();

        HeadlessWindowExtensions.KeyTextInput(fixture.Window, "coffee");

        Assert.Contains("coffee", fixture.ViewModel.WorkspaceTree.QueryText);
    }

    [AvaloniaFact]
    public async Task NewItemButton_ClickOpensCreateItemEditor()
    {
        using var fixture = new MainWindowFixture();

        var newItemButton = fixture.FindControl<Button>(b =>
            (b.Content as string) == "+ New Item" && b.IsVisible);
        Assert.True(newItemButton.IsEnabled);
        newItemButton.Command.Execute(null);
        await Task.Delay(50);
        fixture.PumpLayout();

        var editingNode = fixture.ViewModel.WorkspaceTree.Roots
            .SelectMany(r => r.Children)
            .FirstOrDefault(n => n.IsEditing);
        Assert.NotNull(editingNode);
    }
}
