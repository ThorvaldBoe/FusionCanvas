using FusionCanvas.App.Snowclones;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.App.Tests.Snowclones;

public sealed class SnowcloneLibraryViewModelTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-02T03:04:05Z");

    [Fact]
    public async Task OpenAsync_PreselectsFirstAlphabeticalSnowclone()
    {
        var alpha = Snowclone("Alpha {X}", "First");
        var beta = Snowclone("beta {Y}", "Second");
        var fixture = Fixture([beta, alpha]);

        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);

        Assert.True(fixture.ViewModel.IsLoaded);
        Assert.Equal([alpha.Id, beta.Id], fixture.ViewModel.Snowclones.Select(item => item.Id));
        Assert.Equal(alpha.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal(alpha.Phrase, fixture.ViewModel.Phrase);
    }

    [Fact]
    public async Task Search_WhenSelectionIsFilteredOut_PreservesEditorDraft()
    {
        var alpha = Snowclone("Alpha {X}", "First");
        var beta = Snowclone("Beta {Y}", "Second");
        var fixture = Fixture([alpha, beta]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.Guidance = "Unsaved draft";

        fixture.ViewModel.SearchText = "Beta";
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(beta.Id, Assert.Single(fixture.ViewModel.Snowclones).Id);
        Assert.Equal(alpha.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal("Unsaved draft", fixture.ViewModel.Guidance);
    }

    [Fact]
    public async Task NewAndSave_CreatesDraftThenSelectsPersistedRecord()
    {
        var fixture = Fixture([]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        var focusRequested = false;
        fixture.ViewModel.FocusPhraseRequested += (_, _) => focusRequested = true;

        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();
        fixture.ViewModel.Phrase = "New {X}";
        fixture.ViewModel.Guidance = "Guidance";

        Assert.True(fixture.ViewModel.IsNewDraft);
        Assert.True(focusRequested);
        Assert.True(fixture.ViewModel.CanSave);
        Assert.False(fixture.ViewModel.CanDelete);

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.False(fixture.ViewModel.IsNewDraft);
        Assert.Equal("New {X}", fixture.ViewModel.SelectedSnowclone!.Phrase);
        Assert.Single(fixture.Repository.Snapshot.Snowclones);
    }

    [Fact]
    public async Task MeaningfulDraft_SelectionTransitionSupportsCancelAndDiscard()
    {
        var first = Snowclone("First {X}", "First");
        var second = Snowclone("Second {Y}", "Second");
        var fixture = Fixture([first, second]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.Guidance = "Unsaved";

        fixture.ViewModel.SelectSnowcloneCommand.Execute(
            fixture.ViewModel.Snowclones.Single(item => item.Id == second.Id));

        Assert.True(fixture.ViewModel.UnsavedPromptVisible);
        fixture.ViewModel.CancelPendingCommand.Execute(null);
        Assert.Equal(first.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal("Unsaved", fixture.ViewModel.Guidance);

        fixture.ViewModel.SelectSnowcloneCommand.Execute(
            fixture.ViewModel.Snowclones.Single(item => item.Id == second.Id));
        fixture.ViewModel.DiscardAndContinueCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(second.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal("Second", fixture.ViewModel.Guidance);
    }

    [Fact]
    public async Task BlankNewDraft_SelectionChangeDiscardsWithoutPrompt()
    {
        var existing = Snowclone("Existing {X}", "Guidance");
        var fixture = Fixture([existing]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        fixture.ViewModel.SelectSnowcloneCommand.Execute(fixture.ViewModel.Snowclones[0]);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.False(fixture.ViewModel.UnsavedPromptVisible);
        Assert.Equal(existing.Id, fixture.ViewModel.SelectedSnowclone!.Id);
    }

    [Fact]
    public async Task CloseWithDraft_SaveAndContinuePersistsThenRequestsClose()
    {
        var existing = Snowclone("Existing {X}", "Guidance");
        var fixture = Fixture([existing]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.Guidance = "Saved edit";
        var closeRequested = false;
        fixture.ViewModel.CloseRequested += (_, _) => closeRequested = true;

        fixture.ViewModel.RequestClose();
        Assert.True(fixture.ViewModel.UnsavedPromptVisible);

        fixture.ViewModel.SaveAndContinueCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(closeRequested);
        Assert.Equal("Saved edit", Assert.Single(fixture.Repository.Snapshot.Snowclones).Guidance);
    }

    [Fact]
    public async Task Delete_CancelKeepsRecordAndConfirmSelectsRemainingRecord()
    {
        var first = Snowclone("First {X}", "First");
        var second = Snowclone("Second {Y}", "Second");
        var fixture = Fixture([first, second]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);

        fixture.ViewModel.RequestDeleteCommand.Execute(null);
        Assert.True(fixture.ViewModel.DeleteConfirmationVisible);
        fixture.ViewModel.CancelDeleteCommand.Execute(null);
        Assert.False(fixture.ViewModel.DeleteConfirmationVisible);
        Assert.Equal(first.Id, fixture.ViewModel.SelectedSnowclone!.Id);

        fixture.ViewModel.RequestDeleteCommand.Execute(null);
        fixture.ViewModel.ConfirmDeleteCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(second.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal(second, Assert.Single(fixture.Repository.Snapshot.Snowclones));
    }

    [Fact]
    public async Task CancelledPicker_PreservesSelectionSearchAndDraft()
    {
        var existing = Snowclone("Existing {X}", "Guidance");
        var picker = new StubPicker();
        var fixture = Fixture([existing], picker: picker);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.SearchText = "Existing";
        await fixture.ViewModel.WhenIdleAsync();

        fixture.ViewModel.ImportCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();
        fixture.ViewModel.Guidance = "Recoverable draft";
        fixture.ViewModel.ExportCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(existing.Id, fixture.ViewModel.SelectedSnowclone!.Id);
        Assert.Equal("Existing", fixture.ViewModel.SearchText);
        Assert.Equal("Recoverable draft", fixture.ViewModel.Guidance);
        Assert.Null(fixture.ViewModel.SummaryMessage);
        Assert.Equal(0, fixture.Repository.SaveCalls);
        Assert.Equal(1, picker.ImportCalls);
        Assert.Equal(1, picker.ExportCalls);
    }

    [Fact]
    public async Task ImportWithDraft_CancelPreservesDraftAndDiscardContinues()
    {
        var existing = Snowclone("Existing {X}", "Guidance");
        var picker = new StubPicker { ImportStream = new MemoryStream() };
        var fixture = Fixture([existing], picker);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.Guidance = "Unsaved";

        fixture.ViewModel.ImportCommand.Execute(null);
        Assert.True(fixture.ViewModel.UnsavedPromptVisible);
        fixture.ViewModel.CancelPendingCommand.Execute(null);

        Assert.Equal(0, picker.ImportCalls);
        Assert.Equal("Unsaved", fixture.ViewModel.Guidance);

        fixture.ViewModel.ImportCommand.Execute(null);
        fixture.ViewModel.DiscardAndContinueCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(1, picker.ImportCalls);
        Assert.Equal("Guidance", fixture.ViewModel.Guidance);
    }

    [Fact]
    public async Task BundledImportWithDraft_SaveAndContinuePersistsBothChanges()
    {
        var existing = Snowclone("Existing {X}", "Guidance");
        var codec = new StubCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow("Bundled {Y}", "Bundled guidance", 2)])
        };
        var fixture = Fixture([existing], codec: codec);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.ViewModel.Guidance = "Saved before import";

        fixture.ViewModel.ImportBundledCommand.Execute(null);
        Assert.True(fixture.ViewModel.UnsavedPromptVisible);
        fixture.ViewModel.SaveAndContinueCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(2, fixture.Repository.Snapshot.Snowclones.Count);
        Assert.Contains(
            fixture.Repository.Snapshot.Snowclones,
            item => item.Id == existing.Id && item.Guidance == "Saved before import");
        Assert.Contains(
            fixture.Repository.Snapshot.Snowclones,
            item => item.Phrase == "Bundled {Y}");
    }

    [Fact]
    public async Task Import_ReportsCountsAndRefreshesList()
    {
        var codec = new StubCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow("Imported {X}", "Guidance", 2)])
        };
        var picker = new StubPicker { ImportStream = new MemoryStream() };
        var fixture = Fixture([], picker, codec);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);

        fixture.ViewModel.ImportCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Contains("Imported 1", fixture.ViewModel.SummaryMessage);
        Assert.Equal("Imported {X}", Assert.Single(fixture.ViewModel.Snowclones).Phrase);
    }

    [Fact]
    public async Task ImportBusy_DisablesConflictingActionsUntilCodecCompletes()
    {
        var codec = new StubCodec { DelayRead = true };
        var picker = new StubPicker { ImportStream = new MemoryStream() };
        var fixture = Fixture([], picker, codec);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);

        fixture.ViewModel.ImportCommand.Execute(null);
        await codec.ReadStarted.Task;
        fixture.ViewModel.ImportCommand.Execute(null);

        Assert.True(fixture.ViewModel.IsBusy);
        Assert.False(fixture.ViewModel.CanMutate);
        Assert.Equal(1, picker.ImportCalls);

        codec.CompleteDelayedRead(
            SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow("Delayed {X}", "Guidance", 2)]));
        await fixture.ViewModel.WhenIdleAsync();

        Assert.False(fixture.ViewModel.IsBusy);
        Assert.True(fixture.ViewModel.CanMutate);
    }

    [Fact]
    public async Task SaveFailure_PreservesDraftAndConfirmedLibrary()
    {
        var existing = Snowclone("Existing {X}", "Confirmed");
        var fixture = Fixture([existing]);
        await fixture.ViewModel.OpenAsync(TestContext.Current.CancellationToken);
        fixture.Repository.SaveException = new IOException("disk full");
        fixture.ViewModel.Guidance = "Retry me";

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(fixture.ViewModel.HasError);
        Assert.Contains("disk full", fixture.ViewModel.ErrorMessage);
        Assert.Equal("Retry me", fixture.ViewModel.Guidance);
        Assert.Equal("Confirmed", Assert.Single(fixture.Repository.Snapshot.Snowclones).Guidance);
    }

    private static FixtureState Fixture(
        IReadOnlyList<Snowclone> snowclones,
        StubPicker? picker = null,
        StubCodec? codec = null)
    {
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot(snowclones, true));
        var actualCodec = codec ?? new StubCodec();
        var service = new SnowcloneLibraryService(
            repository,
            actualCodec,
            new StubBundledSource(),
            () => Now.AddHours(1),
            Guid.NewGuid);
        var viewModel = new SnowcloneLibraryViewModel(service, picker ?? new StubPicker());
        return new FixtureState(repository, viewModel);
    }

    private static Snowclone Snowclone(string phrase, string guidance) =>
        new(Guid.NewGuid(), phrase, guidance, Now, Now);

    private sealed record FixtureState(
        InMemoryRepository Repository,
        SnowcloneLibraryViewModel ViewModel);

    private sealed class InMemoryRepository(SnowcloneLibrarySnapshot snapshot) : ISnowcloneRepository
    {
        public SnowcloneLibrarySnapshot Snapshot { get; private set; } = snapshot;

        public int SaveCalls { get; private set; }

        public Exception? SaveException { get; set; }

        public Task<SnowcloneLibrarySnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Snapshot);
        }

        public Task SaveAsync(SnowcloneLibrarySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            if (SaveException is not null)
            {
                throw SaveException;
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCodec : ISnowcloneCsvCodec
    {
        private readonly TaskCompletionSource<SnowcloneCsvReadResult> _delayedRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SnowcloneCsvReadResult ReadResult { get; init; } = SnowcloneCsvReadResult.Success([]);

        public bool DelayRead { get; init; }

        public TaskCompletionSource ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SnowcloneCsvReadResult> ReadAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadStarted.TrySetResult();
            return DelayRead ? _delayedRead.Task : Task.FromResult(ReadResult);
        }

        public Task WriteAsync(
            Stream stream,
            IReadOnlyList<SnowcloneCsvRow> rows,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void CompleteDelayedRead(SnowcloneCsvReadResult result) =>
            _delayedRead.TrySetResult(result);
    }

    private sealed class StubBundledSource : IBundledSnowcloneSource
    {
        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }

    private sealed class StubPicker : ISnowcloneCsvFilePicker
    {
        public Stream? ImportStream { get; init; }

        public Stream? ExportStream { get; init; }

        public int ImportCalls { get; private set; }

        public int ExportCalls { get; private set; }

        public Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImportCalls++;
            return Task.FromResult(ImportStream);
        }

        public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExportCalls++;
            return Task.FromResult(ExportStream);
        }
    }
}
