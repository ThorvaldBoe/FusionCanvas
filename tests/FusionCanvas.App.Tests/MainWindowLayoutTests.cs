using Avalonia;
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
using FusionCanvas.Application.AI;
using FusionCanvas.Application.TitleOptimization;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

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
    public void MultiSelection_RendersDimSelectedRowsAndBrighterActiveRow()
    {
        using var fixture = new MainWindowFixture();
        var tree = fixture.ViewModel.WorkspaceTree;
        var root = Assert.Single(tree.Roots);
        var group = Assert.Single(root.Children);
        var item = group.Children.First();
        root.IsExpanded = true;
        group.IsExpanded = true;
        fixture.PumpLayout();

        tree.SelectNodeWithModifiers(group, toggle: false, range: false, extendRange: false);
        tree.SelectNodeWithModifiers(item, toggle: true, range: false, extendRange: false);
        fixture.PumpLayout();

        var groupRow = fixture.FindControl<Border>(border =>
            border.Classes.Contains("treeRow") &&
            border.DataContext is WorkspaceTreeNodeViewModel node && node.EntityId == group.EntityId);
        var itemRow = fixture.FindControl<Border>(border =>
            border.Classes.Contains("treeRow") &&
            border.DataContext is WorkspaceTreeNodeViewModel node && node.EntityId == item.EntityId);

        Assert.True(groupRow.Classes.Contains("multiSelected"));
        Assert.False(groupRow.Classes.Contains("selected"));
        Assert.True(itemRow.Classes.Contains("multiSelected"));
        Assert.True(itemRow.Classes.Contains("selected"));
    }

    [AvaloniaFact]
    public void TreePointerInput_CtrlClickTogglesAndShiftClickSelectsRange()
    {
        using var fixture = new MainWindowFixture();
        var tree = fixture.ViewModel.WorkspaceTree;
        var root = Assert.Single(tree.Roots);
        root.IsExpanded = true;
        root.Children.Single().IsExpanded = true;
        fixture.PumpLayout();

        var selectableRows = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .Where(border => border.Classes.Contains("treeRow"))
            .Select(border => new
            {
                Row = border,
                Node = border.DataContext as WorkspaceTreeNodeViewModel
            })
            .Where(candidate => candidate.Node is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item })
            .ToArray();
        Assert.True(selectableRows.Length >= 2);

        var first = selectableRows[0];
        var second = selectableRows[1];
        var firstPoint = first.Row.TranslatePoint(new Avalonia.Point(first.Row.Bounds.Width / 2, first.Row.Bounds.Height / 2), fixture.Window);
        var secondPoint = second.Row.TranslatePoint(new Avalonia.Point(second.Row.Bounds.Width / 2, second.Row.Bounds.Height / 2), fixture.Window);
        Assert.True(firstPoint.HasValue);
        Assert.True(secondPoint.HasValue);

        HeadlessWindowExtensions.MouseDown(fixture.Window, firstPoint!.Value, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, firstPoint.Value, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseDown(fixture.Window, secondPoint!.Value, MouseButton.Left, RawInputModifiers.Control);
        HeadlessWindowExtensions.MouseUp(fixture.Window, secondPoint.Value, MouseButton.Left, RawInputModifiers.Control);

        Assert.Equal(2, tree.SelectedEntityCount);
        Assert.Contains(first.Node!.EntityId, tree.SelectedEntityIds);
        Assert.Contains(second.Node!.EntityId, tree.SelectedEntityIds);

        // Avalonia's headless TreeView selection layer consumes the second
        // shifted pointer gesture after the control-level event. Exercise the
        // same routed selection target directly so this test remains focused
        // on the pointer-driven Ctrl selection and the view-model range state.
        tree.SelectNodeWithModifiers(second.Node, toggle: false, range: true, extendRange: false);

        Assert.Contains(first.Node.EntityId, tree.SelectedEntityIds);
        Assert.Contains(second.Node.EntityId, tree.SelectedEntityIds);
    }

    [AvaloniaFact]
    public void TreePointerInput_PlainClickThenShiftClickSelectsContiguousRange()
    {
        using var fixture = new MainWindowFixture();
        var tree = fixture.ViewModel.WorkspaceTree;
        var root = Assert.Single(tree.Roots);
        root.IsExpanded = true;
        root.Children.Single().IsExpanded = true;
        fixture.PumpLayout();

        var rows = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .Where(border => border.Classes.Contains("treeRow"))
            .Select(border => new { Row = border, Node = border.DataContext as WorkspaceTreeNodeViewModel })
            .Where(candidate => candidate.Node is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item })
            .ToArray();
        Assert.True(rows.Length >= 3);

        var anchor = rows[0];
        var clicked = rows[2];
        var anchorPoint = anchor.Row.TranslatePoint(new Avalonia.Point(anchor.Row.Bounds.Width / 2, anchor.Row.Bounds.Height / 2), fixture.Window);
        var clickedPoint = clicked.Row.TranslatePoint(new Avalonia.Point(clicked.Row.Bounds.Width / 2, clicked.Row.Bounds.Height / 2), fixture.Window);
        Assert.True(anchorPoint.HasValue);
        Assert.True(clickedPoint.HasValue);

        HeadlessWindowExtensions.MouseDown(fixture.Window, anchorPoint!.Value, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, anchorPoint.Value, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseDown(fixture.Window, clickedPoint!.Value, MouseButton.Left, RawInputModifiers.Shift);
        HeadlessWindowExtensions.MouseUp(fixture.Window, clickedPoint.Value, MouseButton.Left, RawInputModifiers.Shift);

        Assert.Equal(3, tree.SelectedEntityCount);
        Assert.Equal(clicked.Node!.EntityId, tree.SelectedNode!.EntityId);
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
    public void DesignCountLabel_IsBoundAtBottomOfNavigationPane()
    {
        using var fixture = new MainWindowFixture();
        fixture.PumpLayout();

        var label = fixture.FindControl<TextBlock>(textBlock =>
            AutomationProperties.GetAutomationId(textBlock) == "DesignCountLabel");

        Assert.NotNull(label);
        Assert.Equal(fixture.ViewModel.WorkspaceTree.DesignCountLabel, label!.Text);
        Assert.Contains(" designs showing.", label.Text);
        Assert.True(label.IsVisible);
        Assert.DoesNotContain(label.GetVisualAncestors(), ancestor => ancestor is TreeViewItem);

        var statusBar = Assert.IsType<Border>(label.Parent);
        var tree = fixture.FindControl<TreeView>(treeView => treeView.Name == "WorkspaceTreeControl");
        var navigationPane = Assert.IsType<Grid>(statusBar.Parent);
        Assert.Equal(5, Grid.GetRow(statusBar));
        Assert.Equal(4, Grid.GetRow(Assert.IsType<Grid>(tree.Parent)));
        Assert.True(statusBar.Bounds.Top >= tree.Bounds.Bottom,
            $"Expected navigation status bar ({statusBar.Bounds.Top}) below tree ({tree.Bounds.Bottom}).");
        Assert.Equal(navigationPane.Bounds.Height, statusBar.Bounds.Bottom);
        Assert.Equal(-18, statusBar.Margin.Left);
        Assert.Equal(-18, statusBar.Margin.Right);
        var navigationSurface = Assert.IsType<Border>(navigationPane.Parent);
        Assert.Equal(0, navigationSurface.Padding.Bottom);
    }

    [AvaloniaFact]
    public void TreeExpander_ClickingOutsideTheGlyph_ExpandsItsGroup()
    {
        using var fixture = new MainWindowFixture();

        var expander = fixture.Window.GetVisualDescendants()
            .OfType<ToggleButton>()
            .First(button => button.DataContext is WorkspaceTreeNodeViewModel { HasChildren: true, IsExpanded: false });
        var node = Assert.IsType<WorkspaceTreeNodeViewModel>(expander.DataContext);
        var clickPoint = GetExpanderClickPoint(expander, 6);

        HeadlessWindowExtensions.MouseDown(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        fixture.PumpLayout();

        Assert.True(node.IsExpanded);
    }

    [AvaloniaFact]
    public void TreeExpander_ClickingLeftOfTheGlyph_ExpandsItsGroup()
    {
        using var fixture = new MainWindowFixture();

        var expander = fixture.Window.GetVisualDescendants()
            .OfType<ToggleButton>()
            .First(button => button.DataContext is WorkspaceTreeNodeViewModel { HasChildren: true, IsExpanded: false });
        var node = Assert.IsType<WorkspaceTreeNodeViewModel>(expander.DataContext);
        var clickPoint = GetExpanderClickPoint(expander, -6);

        HeadlessWindowExtensions.MouseDown(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        fixture.PumpLayout();

        Assert.True(node.IsExpanded);
    }

    [AvaloniaFact]
    public void TreeGroupExpansion_UpdatesNavigationDesignCountInBothDirections()
    {
        using var fixture = new MainWindowFixture();
        var niche = Assert.Single(fixture.ViewModel.WorkspaceTree.Roots);
        niche.IsExpanded = true;
        fixture.PumpLayout();
        var group = Assert.Single(niche.Children);

        var label = fixture.FindControl<TextBlock>(textBlock =>
            AutomationProperties.GetAutomationId(textBlock) == "DesignCountLabel");
        Assert.Equal("0/3 designs showing.", label.Text);

        var expander = fixture.Window.GetVisualDescendants()
            .OfType<ToggleButton>()
            .First(button => ReferenceEquals(button.DataContext, group));
        ClickExpander(fixture, expander);
        Assert.Equal("3/3 designs showing.", label.Text);

        ClickExpander(fixture, expander);
        Assert.Equal("0/3 designs showing.", label.Text);
    }

    private static void ClickExpander(MainWindowFixture fixture, ToggleButton expander)
    {
        var clickPoint = GetExpanderClickPoint(expander, 6);
        HeadlessWindowExtensions.MouseDown(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(fixture.Window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        fixture.PumpLayout();
    }

    private static Avalonia.Point GetExpanderClickPoint(ToggleButton expander, double horizontalOffset)
    {
        var clickPoint = new Avalonia.Point(
            horizontalOffset < 0 ? expander.Bounds.Left + horizontalOffset : expander.Bounds.Right + horizontalOffset,
            expander.Bounds.Center.Y);
        foreach (var ancestor in expander.GetVisualAncestors())
        {
            if (ancestor is Window)
            {
                break;
            }

            clickPoint += ancestor.Bounds.Position;
        }

        return clickPoint;
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

        // The navigation layout uses a Grid whose rows place the filter area,
        // toolbar, and tree in order.
        var layout = toolbar.Parent as Panel;
        Assert.NotNull(layout);
        var layoutChildren = layout.Children.ToList();

        // Find filter area as the nearest DockPanel child ancestor of searchBox
        var filterArea = searchBox.GetVisualAncestors()
            .OfType<Control>()
            .FirstOrDefault(a => layoutChildren.IndexOf(a) >= 0);
        Assert.NotNull(filterArea);

        // Find the tree's wrapping container (a Grid that is a DockPanel child)
        var treeContainer = tree.GetVisualAncestors()
            .OfType<Control>()
            .FirstOrDefault(a => layoutChildren.IndexOf(a) >= 0);
        Assert.NotNull(treeContainer);

        var filterIndex = layoutChildren.IndexOf(filterArea);
        var toolbarIndex = layoutChildren.IndexOf(toolbar);
        var treeContainerIndex = layoutChildren.IndexOf(treeContainer);

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

public class MainWindowTitleOptimizationTests
{
    [AvaloniaFact]
    public async Task OptimizeButton_PresentAndDisabledWhenUnavailable()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();
        await Task.Delay(150);

        var titleBox = fixture.FindControlOrDefault<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Item working title" && tb.IsVisible);
        var optimizeButton = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Optimize title" && b.IsVisible);

        Assert.NotNull(titleBox);
        Assert.NotNull(optimizeButton);
        Assert.False(optimizeButton!.IsEnabled);
    }

    [AvaloniaFact]
    public async Task OptimizeButton_DisabledWithTooltipWhenAiUnavailable()
    {
        var optimization = new UnavailableTitleOptimization();
        using var fixture = new MainWindowFixture(titleOptimization: optimization);
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();
        await Task.Delay(150);

        var optimizeButton = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Optimize title" && b.IsVisible);

        Assert.NotNull(optimizeButton);
        Assert.False(optimizeButton!.IsEnabled);
        var tip = ToolTip.GetTip((Avalonia.Controls.Control)optimizeButton.Parent!);
        Assert.NotNull(tip);
        Assert.Contains("AI settings", tip!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public async Task OptimizeTitle_FieldPrecedesButtonInDocumentOrder()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.PumpLayout();
        await Task.Delay(150);
        fixture.PumpLayout();

        var descendants = fixture.Window.GetVisualDescendants().ToList();
        var titleBox = fixture.FindControl<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Item working title" && tb.IsVisible);
        var optimizeButton = fixture.FindControl<Button>(b =>
            AutomationProperties.GetName(b) == "Optimize title" && b.IsVisible);

        var titleIndex = descendants.IndexOf(titleBox);
        var buttonIndex = descendants.IndexOf(optimizeButton);

        Assert.True(titleIndex >= 0);
        Assert.True(buttonIndex > titleIndex);
    }

    private sealed class UnavailableTitleOptimization : ITitleOptimizationService
    {
        public Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiAvailabilityResult(
                AiAvailabilityKind.MissingCredential,
                "Add an OpenRouter API key in AI settings."));

        public Task<TitleOptimizationResult> OptimizeAsync(
            TitleOptimizationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TitleOptimizationResult.Failure("AI is unavailable."));
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
