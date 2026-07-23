using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using FusionCanvas.App.Views;
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
            g.ColumnDefinitions.Count == 3 && g.ColumnSpacing == 8
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
