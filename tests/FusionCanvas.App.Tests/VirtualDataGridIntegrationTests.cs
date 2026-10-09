using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaVirtualDataGrid.Columns;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;
using AvaloniaVirtualDataGrid.Services;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Application.Catalog;

namespace FusionCanvas.App.Tests;

public sealed class VirtualDataGridIntegrationTests
{
    [AvaloniaFact]
    public async Task Imported_grid_renders_bulk_preview_in_aligned_columns_with_a_custom_template()
    {
        var candidates = new InMemoryDataProvider<BulkVariantCandidate>(
        [
            new(Guid.NewGuid(), "S", true, null),
            new(Guid.NewGuid(), "Extra large", false, "Already configured")
        ]);
        var grid = new VirtualDataGrid
        {
            ItemsSource = candidates,
            SelectionMode = DataGridSelectionMode.None
        };
        grid.Columns.Add(new VirtualDataGridTextColumn("Size", item => ((BulkVariantCandidate)item!).SizeName)
        {
            Width = 140,
            IsSortable = false
        });
        grid.Columns.Add(new VirtualDataGridTemplateColumn
        {
            Header = "Exclusion reason",
            Width = 280,
            IsSortable = false,
            CellTemplate = new FuncDataTemplate<BulkVariantCandidate>((item, _) => new TextBlock
            {
                Text = item?.ExclusionReason ?? string.Empty,
                Name = "PreviewReason"
            })
        });
        var window = new Window { Width = 460, Height = 220, Content = grid };

        try
        {
            window.Show();
            window.UpdateLayout();
            await HeadlessUiWait.UntilAsync(
                () => grid.GetVisualDescendants().OfType<VirtualDataRow>().Count() == 2,
                "imported grid realizes both preview rows");
            window.UpdateLayout();

            Assert.NotNull(grid.Template);
            var rows = grid.GetVisualDescendants().OfType<VirtualDataRow>().ToArray();
            Assert.All(rows, row => Assert.Equal(2, row.Cells.Count));
            Assert.Equal("S", Assert.IsType<TextBlock>(rows[0].Cells[0].Content).Text);
            Assert.Equal("Extra large", Assert.IsType<TextBlock>(rows[1].Cells[0].Content).Text);
            Assert.Equal("Already configured", Assert.IsType<TextBlock>(rows[1].Cells[1].Content).Text);
            Assert.True(rows[0].Cells[0].Bounds.Width > 0);
            Assert.Equal(rows[0].Cells[0].Bounds.Width, rows[1].Cells[0].Bounds.Width);
            Assert.Equal(rows[0].Cells[1].Bounds.X, rows[1].Cells[1].Bounds.X);

            candidates.Reset([new(Guid.NewGuid(), "M", false, "Provider unavailable")]);
            window.UpdateLayout();
            await HeadlessUiWait.UntilAsync(
                () => grid.GetVisualDescendants().OfType<VirtualDataRow>().Count() == 1,
                "grid refreshes the replaced preview snapshot");
            var refreshedRow = Assert.Single(grid.GetVisualDescendants().OfType<VirtualDataRow>());
            Assert.Equal("M", Assert.IsType<TextBlock>(refreshedRow.Cells[0].Content).Text);
            Assert.Equal("Provider unavailable", Assert.IsType<TextBlock>(refreshedRow.Cells[1].Content).Text);
        }
        finally
        {
            window.Close();
        }
    }
}
