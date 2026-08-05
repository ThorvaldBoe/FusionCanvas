using System.Text;
using FusionCanvas.App.Items.Import;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.Items.Import;

public sealed class ItemImportViewModelTests
{
    private static readonly ItemTopicReference Target =
        new(WorkspaceEntityKind.Niche, Guid.NewGuid());

    [Fact]
    public void RunPreview_PopulatesPreviewAndEnablesImport()
    {
        var codec = new FakeCodec(ValidResult(["Alpha", "Beta"]));
        var vm = Create(codec: codec);
        vm.RawSource = "row";

        vm.RunPreview();

        Assert.Equal(2, vm.PreviewRows.Count);
        Assert.True(vm.CanImport);
        Assert.False(vm.HasErrors);
    }

    [Fact]
    public void RunPreview_WithErrorDisablesImport()
    {
        var codec = new FakeCodec(new ItemCsvParseResult([], [new ItemCsvParseError(2, "bad")]));
        var vm = Create(codec: codec);
        vm.RawSource = "row";

        vm.RunPreview();

        Assert.False(vm.CanImport);
        Assert.True(vm.HasErrors);
        Assert.Equal(["Error on line 2"], vm.ErrorMessages);
    }

    [Fact]
    public void RunPreview_EmptySourceDisablesImport()
    {
        var codec = new FakeCodec(new ItemCsvParseResult([], []));
        var vm = Create(codec: codec);

        vm.RunPreview();

        Assert.False(vm.CanImport);
        Assert.Empty(vm.PreviewRows);
    }

    [Fact]
    public async Task PickFile_HydratesRawSourceAndRunsPreview()
    {
        const string csv = "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\nAlpha;A;B;C;D;E;f\n";
        var picker = new FakeFilePicker { ImportStream = Stream(csv) };
        var codec = new FakeCodec(ValidResult(["Alpha"]));
        var vm = Create(picker: picker, codec: codec);

        vm.PickFileCommand.Execute(null);
        await vm.WhenIdleAsync();

        Assert.Equal(csv, vm.RawSource);
        Assert.True(vm.CanImport);
        Assert.Single(vm.PreviewRows);
    }

    [Fact]
    public async Task PickFile_DecoderFailure_SurfacesLoadError()
    {
        var picker = new FakeFilePicker { ImportStream = new MemoryStream([0xC3, 0x28]) };
        var vm = Create(picker: picker, codec: new FakeCodec(ValidResult([])));

        vm.PickFileCommand.Execute(null);
        await vm.WhenIdleAsync();

        Assert.True(vm.HasLoadError);
        Assert.Contains("UTF-8", vm.LoadError);
    }

    [Fact]
    public async Task Import_InvokesServiceAndSetsCompletion()
    {
        var service = new FakeImportService { Result = ItemCsvImportResult.Success(1) };
        var vm = Create(service: service, codec: new FakeCodec(ValidResult(["Alpha"])));
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;
        vm.RawSource = "row";
        vm.RunPreview();

        vm.ImportCommand.Execute(null);
        await vm.WhenIdleAsync();

        Assert.True(service.Called);
        Assert.Equal(1, service.LastRequest!.Rows.Count);
        Assert.True(vm.HasImportCompleted);
        Assert.True(closed);
    }

    [Fact]
    public async Task Import_OnFailure_SetsErrorMessageAndDoesNotComplete()
    {
        var service = new FakeImportService { Result = ItemCsvImportResult.Failure("nope") };
        var vm = Create(service: service, codec: new FakeCodec(ValidResult(["Alpha"])));
        vm.RawSource = "row";
        vm.RunPreview();

        vm.ImportCommand.Execute(null);
        await vm.WhenIdleAsync();

        Assert.True(service.Called);
        Assert.False(vm.HasImportCompleted);
        Assert.Equal("nope", vm.ErrorMessage);
    }

    [Fact]
    public async Task Import_DisabledWhenNoValidRows_DoesNotCallService()
    {
        var service = new FakeImportService { Result = ItemCsvImportResult.Success(0) };
        var vm = Create(service: service, codec: new FakeCodec(new ItemCsvParseResult([], [new ItemCsvParseError(1, "x")])));
        vm.RawSource = "row";
        vm.RunPreview();

        vm.ImportCommand.Execute(null);
        await vm.WhenIdleAsync();

        Assert.False(service.Called);
        Assert.False(vm.HasImportCompleted);
    }

    [Fact]
    public async Task ExportSample_WritesCodecSampleToExportStream()
    {
        var picker = new FakeFilePicker { ExportStream = new MemoryStream() };
        var codec = new FakeCodec(ValidResult([])) { Sample = "Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags\n" };
        var vm = Create(codec: codec, picker: picker);

        vm.ExportSampleCommand.Execute(null);
        await vm.WhenIdleAsync();

        var text = Encoding.UTF8.GetString(picker.ExportStream!.ToArray());
        Assert.Equal(codec.Sample, text);
    }

    [Fact]
    public void Close_RaisesCloseRequestedWithoutMutation()
    {
        var service = new FakeImportService { Result = ItemCsvImportResult.Success(1) };
        var vm = Create(service: service, codec: new FakeCodec(ValidResult(["Alpha"])));
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closed);
        Assert.False(service.Called);
    }

    private static ItemImportViewModel Create(
        FakeImportService? service = null,
        FakeCodec? codec = null,
        FakeFilePicker? picker = null) =>
        new(
            Target,
            "Niche",
            service ?? new FakeImportService { Result = ItemCsvImportResult.Success(1) },
            codec ?? new FakeCodec(ValidResult([])),
            picker ?? new FakeFilePicker());

    private static ItemCsvParseResult ValidResult(IReadOnlyList<string> titles) =>
        new(
            titles.Select((title, index) => new ItemCsvRow(title, null, null, null, null, null, [], index + 1)).ToArray(),
            []);

    private static MemoryStream Stream(string text) =>
        new(new UTF8Encoding(false).GetBytes(text));

    private sealed class FakeCodec : IItemCsvCodec
    {
        private readonly ItemCsvParseResult _result;

        public FakeCodec(ItemCsvParseResult result) => _result = result;

        public string Sample { get; set; } = "sample";

        public ItemCsvParseResult Parse(string source) => _result;

        public string WriteSample() => Sample;
    }

    private sealed class FakeImportService : IItemCsvImportService
    {
        public ItemCsvImportResult Result { get; set; } = ItemCsvImportResult.Success(0);
        public bool Called { get; private set; }
        public ItemCsvImportRequest? LastRequest { get; private set; }

        public Task<ItemCsvImportResult> ImportAsync(ItemCsvImportRequest request, CancellationToken cancellationToken = default)
        {
            Called = true;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeFilePicker : IItemCsvFilePicker
    {
        public Stream? ImportStream { get; set; }
        public MemoryStream? ExportStream { get; set; }

        public Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ImportStream);

        public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(ExportStream);
    }
}
