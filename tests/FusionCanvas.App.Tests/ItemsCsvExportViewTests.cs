using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Items;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class ItemsCsvExportViewTests
{
    private const string ExportHeader = "Export to CSV...";

    [AvaloniaFact]
    public void ExportMenuShowsOnNicheAndGroupRows_AndHidesOnItemRows()
    {
        using var fixture = new MainWindowFixture();
        ExpandAll(fixture.ViewModel.WorkspaceTree.Roots);
        fixture.PumpLayout();

        var rowMenus = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .Where(border => border.GetValue(ContextMenu.ContextMenuProperty) is not null)
            .Where(border => border.DataContext is WorkspaceTreeNodeViewModel)
            .Select(border => (Node: (WorkspaceTreeNodeViewModel)border.DataContext!,
                Menu: border.GetValue(ContextMenu.ContextMenuProperty)!))
            .ToArray();

        var uniqueNodes = rowMenus
            .GroupBy(entry => entry.Node.EntityKind)
            .Select(group => group.First())
            .ToArray();
        Assert.Contains(uniqueNodes, entry => entry.Node.EntityKind == WorkspaceEntityKind.Niche);
        Assert.Contains(uniqueNodes, entry => entry.Node.EntityKind == WorkspaceEntityKind.Group);
        Assert.Contains(uniqueNodes, entry => entry.Node.EntityKind == WorkspaceEntityKind.Item);

        foreach (var (node, menu) in uniqueNodes)
        {
            menu.Open();
            fixture.PumpLayout();
            var exportItem = EnumerateMenuItems(menu)
                .FirstOrDefault(item => Equals(item.Header, ExportHeader));
            Assert.NotNull(exportItem);

            if (node.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group)
            {
                Assert.True(exportItem!.IsVisible, $"Export item should be visible on {node.KindLabel} row");
            }
            else
            {
                Assert.False(exportItem!.IsVisible, $"Export item should be hidden on {node.KindLabel} row");
            }
        }
    }

    [AvaloniaFact]
    public void ExportMenuClick_InvokesExportThroughTreeViewModel()
    {
        using var fixture = new MainWindowFixture();
        var recorder = new RecordingItemCsvFilePicker();
        fixture.ViewModel.WorkspaceTree.FilePicker = recorder;
        ExpandAll(fixture.ViewModel.WorkspaceTree.Roots);
        fixture.PumpLayout();

        var groupRow = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .Where(border => border.GetValue(ContextMenu.ContextMenuProperty) is not null)
            .Where(border => border.DataContext is WorkspaceTreeNodeViewModel node
                && node.EntityKind == WorkspaceEntityKind.Group)
            .First();
        var menu = groupRow.GetValue(ContextMenu.ContextMenuProperty)!;
        menu.Open();
        fixture.PumpLayout();
        var exportItem = EnumerateMenuItems(menu).First(item => Equals(item.Header, ExportHeader));

        exportItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent) { Source = exportItem });
        fixture.PumpLayout();

        Assert.True(recorder.OpenExportInvoked);
    }

    private static void ExpandAll(IEnumerable<WorkspaceTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            ExpandAll(node.Children);
            node.IsExpanded = true;
        }
    }

    private static IEnumerable<MenuItem> EnumerateMenuItems(ContextMenu menu)
    {
        foreach (var item in menu.Items)
        {
            if (item is MenuItem menuItem)
            {
                yield return menuItem;
            }
        }
    }

    private sealed class RecordingItemCsvFilePicker : IItemCsvFilePicker
    {
        public bool OpenExportInvoked { get; private set; }

        public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
        {
            OpenExportInvoked = true;
            return Task.FromResult<Stream?>(null);
        }
    }
}
