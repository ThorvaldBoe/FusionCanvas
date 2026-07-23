using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class MainWindowLayoutTests
{
    private static MainWindow CreateShownWindow(out MainWindowViewModel viewModel)
    {
        viewModel = new MainWindowViewModel();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        return window;
    }

    private static void Layout(Control window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }

    private static ScrollViewer? FindDetailsScrollViewer(Control window) =>
        window.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(sv =>
            sv.IsVisible
            && sv.Content is StackPanel
            && sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto
            && sv.Bounds.Height > 0);

    [AvaloniaFact]
    public void DetailsColumn_ScrollsWhenContentExceedsMinimumHeight()
    {
        var window = CreateShownWindow(out var viewModel);
        try
        {
            var itemContext = viewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Item);
            viewModel.OpenFromNavigation(itemContext);
            window.Width = 900;
            window.Height = 400;
            Layout(window);

            var scroller = FindDetailsScrollViewer(window);
            Assert.NotNull(scroller);
            Assert.True(scroller!.Extent.Height > scroller.Viewport.Height,
                $"Expected details extent ({scroller.Extent.Height}) to exceed viewport ({scroller.Viewport.Height}) at minimum height.");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FilterControls_StayWithinPaneAtMinimumWidth()
    {
        var window = CreateShownWindow(out _);
        try
        {
            window.Width = 900;
            window.Height = 600;
            Layout(window);

            var searchBox = window.GetVisualDescendants().OfType<TextBox>().First(tb => tb.Name == "TreeSearchBox");
            var stageFilter = window.GetVisualDescendants().OfType<ComboBox>().First(cb => cb.Name == "StageFilter");
            var statusFilter = window.GetVisualDescendants().OfType<ComboBox>().First(cb => cb.Name == "StatusFilter");

            var paneBorder = searchBox.GetVisualAncestors().OfType<Border>()
                .First(b => b.Bounds.Width > 0 && b.Bounds.Width < 600);
            var paneRight = paneBorder.Bounds.Left + paneBorder.Bounds.Width;

            Assert.True(stageFilter.Bounds.Right <= paneRight + 1,
                $"Stage filter extends beyond pane: {stageFilter.Bounds.Right} > {paneRight}");
            Assert.True(statusFilter.Bounds.Right <= paneRight + 1,
                $"Status filter extends beyond pane: {statusFilter.Bounds.Right} > {paneRight}");
            Assert.True(searchBox.Bounds.Right <= paneRight + 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeaderStageLabelAndStatusSelectorHaveHorizontalGap()
    {
        var window = CreateShownWindow(out var viewModel);
        try
        {
            var itemContext = viewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Item);
            viewModel.OpenFromNavigation(itemContext);
            window.Width = 1180;
            window.Height = 760;
            Layout(window);

            var headerGrid = window.GetVisualDescendants().OfType<Grid>()
                .First(g => g.ColumnDefinitions.Count == 3 && g.ColumnSpacing == 8
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
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ItemTextField_LostFocusCommitsEdit()
    {
        var window = CreateShownWindow(out var viewModel);
        try
        {
            var itemContext = viewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Item);
            viewModel.OpenFromNavigation(itemContext);
            window.Width = 1180;
            window.Height = 760;
            Layout(window);
            await Task.Delay(150);

            viewModel.ItemInspector.Notes = "headless edit";
            Assert.True(viewModel.ItemInspector.HasUnsavedChanges);

            var notesBox = window.GetVisualDescendants().OfType<TextBox>()
                .First(tb => AutomationProperties.GetName(tb) == "Item notes" && tb.IsVisible);
            notesBox.Focus();
            Layout(window);

            var other = window.GetVisualDescendants().OfType<TextBox>()
                .First(tb => AutomationProperties.GetName(tb) == "Item working title" && tb.IsVisible);
            other.Focus();
            Layout(window);
            await Task.Delay(150);

            Assert.False(viewModel.ItemInspector.HasUnsavedChanges);
            Assert.Equal("headless edit", viewModel.ItemInspector.State!.Notes);
        }
        finally
        {
            window.Close();
        }
    }
}
