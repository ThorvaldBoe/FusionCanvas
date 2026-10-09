using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;
using AvaloniaVirtualDataGrid.Services;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;

namespace FusionCanvas.App.Tests;

public sealed class BulkVariantPreviewHeadlessTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Preview_templates_align_columns_and_scroll_with_full_text_available(bool dark)
    {
        var catalog = CreateCatalog();
        for (var index = 0; index < 20; index++)
        {
            catalog.BulkPreviewCandidates.Add(new(Guid.NewGuid(),
                index == 0 ? "Extra extra large with a long label" : $"Size {index}",
                index != 1, index == 1 ? "The provider catalog does not allow this Color and Size combination." : null));
        }
        var window = new BulkAddVariantsWindow
        {
            DataContext = catalog,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };

        try
        {
            window.Show();
            var grid = window.FindControl<VirtualDataGrid>("BulkPreviewGrid")!;
            await SettleRows(window, grid, () => Rows(grid).Length >= 2);
            var rows = Rows(grid);
            Assert.InRange(rows.Length, 2, 19);
            Assert.Equal(180, grid.Bounds.Height);
            Assert.Equal(DataGridSelectionMode.None, grid.SelectionMode);
            Assert.All(grid.Columns, column =>
            {
                Assert.False(column.IsSortable);
                Assert.False(column.IsResizable);
            });
            Assert.False(Assert.Single(grid.GetVisualDescendants().OfType<VirtualDataGridHeaderPanel>()).IsHitTestVisible);
            Assert.All(rows, row => Assert.Equal(2, row.Cells.Count));
            Assert.Equal(rows[0].Cells[1].Bounds.X, rows[1].Cells[1].Bounds.X);
            Assert.True(rows[0].Cells[0].Bounds.Width > 0);
            var size = Assert.IsType<TextBlock>(rows[0].Cells[0].Content);
            Assert.Equal(catalog.BulkPreviewCandidates[0].SizeName, size.Text);
            Assert.Equal(size.Text, ToolTip.GetTip(size));
            var reason = Assert.IsType<TextBlock>(rows[1].Cells[1].Content);
            Assert.Equal(catalog.BulkPreviewCandidates[1].ExclusionReason, reason.Text);
            Assert.Equal(reason.Text, ToolTip.GetTip(reason));

            var scroll = Assert.Single(grid.GetVisualDescendants().OfType<ScrollViewer>());
            Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
            Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
            Assert.True(scroll.Viewport.Width >= grid.Columns.Sum(column => column.ActualWidth));
            scroll.ScrollToEnd();
            await SettleRows(window, grid, () => Rows(grid).Any(row => Equals(row.DataContext, catalog.BulkPreviewCandidates[^1])));
            Assert.Contains(Rows(grid), row => Assert.IsType<TextBlock>(row.Cells[0].Content).Text == "Size 19");
            Assert.DoesNotContain(Rows(grid), row => Equals(row.DataContext, catalog.BulkPreviewCandidates[0]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Preview_refreshes_after_rebind_and_detaches_when_closed()
    {
        var first = CreateCatalog();
        first.BulkPreviewCandidates.Add(new(Guid.NewGuid(), "Old size", true, null));
        var second = CreateCatalog();
        second.BulkPreviewCandidates.Add(new(Guid.NewGuid(), "New size", false, "Already configured"));
        var window = new BulkAddVariantsWindow { DataContext = first };

        try
        {
            window.Show();
            var grid = window.FindControl<VirtualDataGrid>("BulkPreviewGrid")!;
            await SettleRows(window, grid, () => Rows(grid).Length == 1);
            window.DataContext = second;
            first.BulkPreviewCandidates.Add(new(Guid.NewGuid(), "Stale size", true, null));
            await SettleRows(window, grid, () => Rows(grid).Length == 1);
            Assert.Equal("New size", Assert.IsType<TextBlock>(Assert.Single(Rows(grid)).Cells[0].Content).Text);

            second.BulkPreviewCandidates.Clear();
            second.BulkPreviewCandidates.Add(new(Guid.NewGuid(), "Refreshed size", true, null));
            await SettleRows(window, grid, () => Rows(grid).Length == 1);
            Assert.Equal("Refreshed size", Assert.IsType<TextBlock>(Assert.Single(Rows(grid)).Cells[0].Content).Text);

            window.Close();
            second.BulkPreviewCandidates.Add(new(Guid.NewGuid(), "After close", true, null));
            Assert.Empty(Assert.IsType<InMemoryDataProvider<BulkVariantCandidate>>(grid.ItemsSource));
        }
        finally
        {
            window.Close();
        }
    }

    private static CatalogSetupViewModel CreateCatalog()
    {
        var repository = new InMemoryWorkspaceRepository(SampleWorkspace.Create());
        return new(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));
    }

    private static VirtualDataRow[] Rows(VirtualDataGrid grid) =>
        grid.GetVisualDescendants().OfType<VirtualDataRow>().ToArray();

    private static Task SettleRows(Window window, VirtualDataGrid grid, Func<bool> condition) =>
        HeadlessUiWait.UntilAsync(() =>
        {
            window.UpdateLayout();
            return grid.Template is not null && condition();
        }, "bulk preview renders the current rows");
}
