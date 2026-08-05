using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Items.Import;
using FusionCanvas.App.Navigation;
using FusionCanvas.Application.Items;
using IItemCsvCodec = FusionCanvas.Application.Items.Import.IItemCsvCodec;
using ItemCsvRow = FusionCanvas.Application.Items.Import.ItemCsvRow;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.Items.Import;

public sealed class ItemImportWindowTests
{
    private static readonly ItemTopicReference Target = new(WorkspaceEntityKind.Niche, Guid.NewGuid());

    [AvaloniaFact]
    public void Window_ConstructsWithRequiredControls()
    {
        var viewModel = new ItemImportViewModel(
            Target, "Niche", new FakeImportService(), new FakeCodec(ValidResult(["Alpha"])));
        var window = new ItemImportWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.NotNull(window.FindControl<TextBox>("RawSourceBox"));
            Assert.NotNull(window.FindControl<ItemsControl>("PreviewList"));
            Assert.NotNull(FindButton(window, "Pick CSV file\u2026"));
            Assert.NotNull(FindButton(window, "Export sample"));
            Assert.NotNull(FindButton(window, "Run preview"));
            Assert.NotNull(FindButton(window, "Import"));
            Assert.NotNull(FindButton(window, "Close"));
            Assert.Equal("Niche", viewModel.TargetLabel);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ImportButtonIsEnabledOnlyWhenPreviewIsValid()
    {
        var valid = new ItemImportViewModel(
            Target, "Niche", new FakeImportService(), new FakeCodec(ValidResult(["Alpha", "Beta"])));
        var window = new ItemImportWindow { DataContext = valid };
        try
        {
            window.Show();
            PumpLayout(window);

            valid.RawSource = "row";
            valid.RunPreview();
            PumpLayout(window);

            var import = FindButton(window, "Import");
            Assert.NotNull(import);
            Assert.True(import.IsEnabled);
            Assert.True(valid.CanImport);
        }
        finally
        {
            window.Close();
        }

        var invalid = new ItemImportViewModel(
            Target, "Niche", new FakeImportService(), new FakeCodec(new ItemCsvParseResult([], [new ItemCsvParseError(2, "bad")])));
        var errorWindow = new ItemImportWindow { DataContext = invalid };
        try
        {
            errorWindow.Show();
            PumpLayout(errorWindow);

            invalid.RawSource = "row";
            invalid.RunPreview();
            PumpLayout(errorWindow);

            var import = FindButton(errorWindow, "Import");
            Assert.NotNull(import);
            Assert.False(import.IsEnabled);
            Assert.False(invalid.CanImport);
        }
        finally
        {
            errorWindow.Close();
        }
    }

    [AvaloniaFact]
    public void PreviewListReflectsParsedRows()
    {
        var viewModel = new ItemImportViewModel(
            Target, "Niche", new FakeImportService(), new FakeCodec(ValidResult(["Alpha", "Beta"])));
        var window = new ItemImportWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            viewModel.RawSource = "row";
            viewModel.RunPreview();
            PumpLayout(window);

            var list = window.FindControl<ItemsControl>("PreviewList");
            Assert.NotNull(list);
            Assert.Equal(2, list.ItemCount);
            Assert.Equal(2, viewModel.PreviewRows.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ContextMenuImportTargetsTopicRows_NicheAndGroupYesItemNo()
    {
        using var fixture = new MainWindowFixture();
        var roots = fixture.ViewModel.WorkspaceTree.Roots;
        Assert.NotEmpty(roots);

        var allNodes = roots.SelectMany(Flatten).ToArray();
        Assert.All(allNodes.Where(node => node.IsGroup || node.EntityKind == WorkspaceEntityKind.Niche),
            node => Assert.True(node.IsTopic));
        Assert.All(allNodes.Where(node => node.IsItem),
            node => Assert.False(node.IsTopic));
        Assert.Contains(allNodes, node => node.EntityKind == WorkspaceEntityKind.Niche);
        Assert.Contains(allNodes, node => node.IsGroup);
    }

    private static IEnumerable<WorkspaceTreeNodeViewModel> Flatten(WorkspaceTreeNodeViewModel node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static Button? FindButton(Control root, string content) =>
        root.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => Equals(button.Content, content));

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }

    private static ItemCsvParseResult ValidResult(IReadOnlyList<string> titles) =>
        new(
            titles.Select((title, index) => new ItemCsvRow(title, null, null, null, null, null, [], index + 1)).ToArray(),
            []);

    private sealed class FakeImportService : IItemCsvImportService
    {
        public Task<ItemCsvImportResult> ImportAsync(ItemCsvImportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(ItemCsvImportResult.Success(request.Rows.Count));
    }

    private sealed class FakeCodec : IItemCsvCodec
    {
        private readonly ItemCsvParseResult _result;

        public FakeCodec(ItemCsvParseResult result) => _result = result;

        public ItemCsvParseResult Parse(string source) => _result;

        public string WriteSample() => "sample";
    }
}
