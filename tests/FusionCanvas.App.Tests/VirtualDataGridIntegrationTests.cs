using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaVirtualDataGrid.Columns;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;
using AvaloniaVirtualDataGrid.Services;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.StageTools;
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

    [AvaloniaFact]
    public async Task Template_editors_support_keyboard_navigation_and_two_way_text_input()
    {
        var row = new VariantListingTermsViewModel(Guid.NewGuid(), "Black / Medium", 24.95m, 8.50m);
        var secondRow = new VariantListingTermsViewModel(Guid.NewGuid(), "White / Large", 28m, 9m);
        var provider = new InMemoryDataProvider<VariantListingTermsViewModel>([row, secondRow]);
        var grid = new VirtualDataGrid
        {
            ItemsSource = provider,
            RowHeight = 32,
            SelectionMode = DataGridSelectionMode.None
        };
        grid.Columns.Add(new VirtualDataGridTextColumn("Variant", item => ((VariantListingTermsViewModel)item!).Name)
        {
            Width = 180,
            IsSortable = false
        });
        grid.Columns.Add(new VirtualDataGridTemplateColumn
        {
            Header = "Selling price",
            Width = 140,
            IsSortable = false,
            CellTemplate = new FuncDataTemplate<VariantListingTermsViewModel>((item, _) =>
            {
                var editor = new TextBox { Name = "SellingPriceEditor", Focusable = true, IsTabStop = true };
                editor.Bind(TextBox.TextProperty, new Binding(nameof(VariantListingTermsViewModel.SellingPrice)) { Mode = BindingMode.TwoWay });
                return editor;
            })
        });
        grid.Columns.Add(new VirtualDataGridTemplateColumn
        {
            Header = "Fulfillment cost",
            Width = 140,
            IsSortable = false,
            CellTemplate = new FuncDataTemplate<VariantListingTermsViewModel>((item, _) =>
            {
                var editor = new TextBox { Name = "FulfillmentCostEditor", Focusable = true, IsTabStop = true };
                editor.Bind(TextBox.TextProperty, new Binding(nameof(VariantListingTermsViewModel.FulfillmentCost)) { Mode = BindingMode.TwoWay });
                return editor;
            })
        });
        var window = new Window { Width = 420, Height = 200, Content = grid };

        try
        {
            window.Show();
            window.UpdateLayout();
            await HeadlessUiWait.UntilAsync(
                () => grid.GetVisualDescendants().OfType<VirtualDataRow>().Any(),
                "editable grid realizes its Variant row");
            var realizedRows = grid.GetVisualDescendants().OfType<VirtualDataRow>().OrderBy(value => value.Index).ToArray();
            Assert.Equal(2, realizedRows.Length);
            var firstCells = realizedRows[0].Cells;
            var secondCells = realizedRows[1].Cells;
            Assert.Equal(3, firstCells.Count);
            Assert.Equal(3, secondCells.Count);
            Assert.All(Enumerable.Range(0, 3), column =>
                Assert.Equal(firstCells[column].Bounds.Width, secondCells[column].Bounds.Width));
            Assert.All(Enumerable.Range(0, 3), column =>
                Assert.Equal(firstCells[column].Bounds.X, secondCells[column].Bounds.X));
            var cell = firstCells[1];

            var priceEditor = Assert.Single(cell.GetVisualDescendants().OfType<TextBox>());
            Assert.Equal("24.95", priceEditor.Text);
            var editorCenter = priceEditor.TranslatePoint(new Point(priceEditor.Bounds.Width / 2, priceEditor.Bounds.Height / 2), window);
            Assert.NotNull(editorCenter);
            HeadlessWindowExtensions.MouseDown(window, editorCenter!.Value, MouseButton.Left, RawInputModifiers.None);
            HeadlessWindowExtensions.MouseUp(window, editorCenter.Value, MouseButton.Left, RawInputModifiers.None);
            Assert.True(priceEditor.Focusable);
            Assert.True(priceEditor.IsTabStop);
            Assert.True(priceEditor.IsFocused);
            priceEditor.Text = "27.95";
            Assert.Equal("27.95", row.SellingPrice);
            Assert.Equal("Black / Medium", row.Name);

            var costEditor = Assert.Single(firstCells[2].GetVisualDescendants().OfType<TextBox>());
            HeadlessWindowExtensions.KeyPress(window, Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, string.Empty);
            Assert.True(costEditor.IsFocused);
            costEditor.Text = "9.25";
            Assert.Equal("9.25", row.FulfillmentCost);

            var nextPriceEditor = Assert.Single(secondCells[1].GetVisualDescendants().OfType<TextBox>());
            HeadlessWindowExtensions.KeyPress(window, Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, string.Empty);
            Assert.True(nextPriceEditor.IsFocused);
            nextPriceEditor.Text = "31.50";
            Assert.Equal("31.50", secondRow.SellingPrice);
            Assert.Equal("27.95", row.SellingPrice);

            var secondCostEditor = Assert.Single(secondCells[2].GetVisualDescendants().OfType<TextBox>());
            HeadlessWindowExtensions.KeyPress(window, Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, string.Empty);
            Assert.True(secondCostEditor.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }
}
