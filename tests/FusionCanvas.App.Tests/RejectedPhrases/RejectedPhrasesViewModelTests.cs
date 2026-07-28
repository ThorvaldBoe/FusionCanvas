using FusionCanvas.App.RejectedPhrases;
using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.RejectedPhrases;

public sealed class RejectedPhrasesViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OpenAsync_PreselectsFirstAlphabeticalRejection()
    {
        var sample = Sample.Create();
        var beta = Rejection(sample, "Beta phrase", null);
        var alpha = Rejection(sample, "Alpha phrase", "Reason");
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [beta, alpha] });

        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        Assert.True(fixture.ViewModel.IsLoaded);
        Assert.Equal([alpha.Id, beta.Id], fixture.ViewModel.Rejections.Select(item => item.Id));
        Assert.Equal(alpha.Id, fixture.ViewModel.SelectedRejection!.Id);
        Assert.Equal("Alpha phrase", fixture.ViewModel.Phrase);
        Assert.Equal("Reason", fixture.ViewModel.Reason);
    }

    [Fact]
    public async Task OpenAsync_WithNoRejections_ShowsEmptyState()
    {
        var sample = Sample.Create();
        var fixture = CreateFixture(sample.Snapshot);

        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        Assert.True(fixture.ViewModel.IsEmpty);
        Assert.Null(fixture.ViewModel.SelectedRejection);
        Assert.False(fixture.ViewModel.CanDelete);
    }

    [Fact]
    public async Task Search_FiltersAcrossPhraseAndReason()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, "Talk to me about pugs", "Off-brand");
        var second = Rejection(sample, "Cat life", "Too generic");
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [first, second] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.SearchText = "off-brand";
        await fixture.ViewModel.WhenIdleAsync();

        var visible = Assert.Single(fixture.ViewModel.Rejections);
        Assert.Equal("Talk to me about pugs", visible.Text);
        Assert.True(fixture.ViewModel.HasNoResults is false);
    }

    [Fact]
    public async Task Search_WhenSelectionIsFilteredOut_PreservesEditorDraft()
    {
        var sample = Sample.Create();
        var alpha = Rejection(sample, "Alpha phrase", "First");
        var beta = Rejection(sample, "Beta phrase", "Second");
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [alpha, beta] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        fixture.ViewModel.Reason = "Unsaved draft";

        fixture.ViewModel.SearchText = "Beta";
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Equal(beta.Id, Assert.Single(fixture.ViewModel.Rejections).Id);
        Assert.Equal(alpha.Id, fixture.ViewModel.SelectedRejection!.Id);
        Assert.Equal("Unsaved draft", fixture.ViewModel.Reason);
    }

    [Fact]
    public async Task Search_NoResults_ShowsNoResultsState()
    {
        var sample = Sample.Create();
        var only = Rejection(sample, "Phrase", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [only] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.SearchText = "no match";
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(fixture.ViewModel.HasNoResults);
        Assert.Empty(fixture.ViewModel.Rejections);
    }

    [Fact]
    public async Task NewAndSave_CreatesDraftThenPersistsAtActiveScope()
    {
        var sample = Sample.Create();
        var fixture = CreateFixture(sample.Snapshot);
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id),
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var focusRequested = false;
        fixture.ViewModel.FocusPhraseRequested += (_, _) => focusRequested = true;

        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();
        fixture.ViewModel.Phrase = "Talk to me about pugs";
        fixture.ViewModel.Reason = "Off-brand";

        Assert.True(fixture.ViewModel.IsNewDraft);
        Assert.True(focusRequested);
        Assert.True(fixture.ViewModel.CanSave);
        Assert.False(fixture.ViewModel.CanDelete);

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.False(fixture.ViewModel.IsNewDraft);
        Assert.Equal("Talk to me about pugs", fixture.ViewModel.SelectedRejection!.Text);
        var persisted = Assert.Single(fixture.Repository.Snapshot.IdeationRejections);
        Assert.Equal(IdeationMode.Basic, persisted.Mode);
    }

    [Fact]
    public async Task NewAndSave_AtWholeWorkspace_RefusesAndKeepsDraft()
    {
        var sample = Sample.Create();
        var fixture = CreateFixture(sample.Snapshot);
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();
        fixture.ViewModel.Phrase = "Talk to me about pugs";

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(fixture.ViewModel.HasError);
        Assert.True(fixture.ViewModel.IsNewDraft);
        Assert.Equal("Talk to me about pugs", fixture.ViewModel.Phrase);
        Assert.Empty(fixture.Repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task Edit_PreservesSelectionAndAdvancesUpdatedAtOnSave()
    {
        var sample = Sample.Create();
        var existing = new IdeationRejection(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Phrase", null, IdeationMode.Basic, Now);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [existing] }, nextClock: Now.AddMinutes(7));
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.Reason = "New reason";
        Assert.True(fixture.ViewModel.IsDirty);
        Assert.True(fixture.ViewModel.CanSave);

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        var persisted = Assert.Single(fixture.Repository.Snapshot.IdeationRejections);
        Assert.Equal("Phrase", persisted.Text);
        Assert.Equal("New reason", persisted.Reason);
        Assert.Equal(Now.AddMinutes(7), persisted.UpdatedAt);
    }

    [Fact]
    public async Task UnsavedEdit_OnSelection_PromptsSaveDiscardCancel()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, "First", null);
        var second = Rejection(sample, "Second", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [first, second] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.Reason = "Unsaved";
        fixture.ViewModel.SelectRejectionCommand.Execute(
            fixture.ViewModel.Rejections.Single(item => item.Id == second.Id));
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(fixture.ViewModel.UnsavedPromptVisible);
        Assert.Equal(first.Id, fixture.ViewModel.SelectedRejection!.Id);

        fixture.ViewModel.CancelPendingCommand.Execute(null);
        Assert.False(fixture.ViewModel.UnsavedPromptVisible);
        Assert.Equal(first.Id, fixture.ViewModel.SelectedRejection!.Id);
        Assert.Equal("Unsaved", fixture.ViewModel.Reason);
    }

    [Fact]
    public async Task Delete_ConfirmThenCancel_KeepsRecord()
    {
        var sample = Sample.Create();
        var only = Rejection(sample, "Only", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [only] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.RequestDeleteCommand.Execute(null);
        Assert.True(fixture.ViewModel.DeleteConfirmationVisible);

        fixture.ViewModel.CancelDeleteCommand.Execute(null);
        Assert.False(fixture.ViewModel.DeleteConfirmationVisible);
        Assert.Single(fixture.Repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task Delete_Confirmed_RemovesRecordAndSelectsSibling()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, "First", null);
        var second = Rejection(sample, "Second", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [first, second] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        fixture.ViewModel.SelectRejectionCommand.Execute(
            fixture.ViewModel.Rejections.Single(item => item.Id == first.Id));
        await fixture.ViewModel.WhenIdleAsync();

        fixture.ViewModel.RequestDeleteCommand.Execute(null);
        fixture.ViewModel.ConfirmDeleteCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        var remaining = Assert.Single(fixture.Repository.Snapshot.IdeationRejections);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(second.Id, fixture.ViewModel.SelectedRejection!.Id);
    }

    [Fact]
    public async Task Delete_OfLastRecord_ShowsEmptyState()
    {
        var sample = Sample.Create();
        var only = Rejection(sample, "Only", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [only] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.RequestDeleteCommand.Execute(null);
        fixture.ViewModel.ConfirmDeleteCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.Empty(fixture.Repository.Snapshot.IdeationRejections);
        Assert.True(fixture.ViewModel.IsEmpty);
        Assert.Null(fixture.ViewModel.SelectedRejection);
    }

    [Fact]
    public async Task SaveFailure_ReportsRecoverableErrorAndPreservesDraft()
    {
        var sample = Sample.Create();
        var fixture = CreateFixture(sample.Snapshot);
        fixture.Repository.FailSave = true;
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();
        fixture.ViewModel.Phrase = "New phrase";

        fixture.ViewModel.SaveCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        Assert.True(fixture.ViewModel.HasError);
        Assert.True(fixture.ViewModel.IsNewDraft);
        Assert.Equal("New phrase", fixture.ViewModel.Phrase);
    }

    [Fact]
    public async Task NewDraft_BlankCancel_DoesNotPrompt()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, "Existing", null);
        var fixture = CreateFixture(sample.Snapshot with { IdeationRejections = [existing] });
        await fixture.ViewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);

        fixture.ViewModel.NewCommand.Execute(null);
        await fixture.ViewModel.WhenIdleAsync();

        fixture.ViewModel.SelectRejectionCommand.Execute(
            fixture.ViewModel.Rejections.Single(item => item.Id == existing.Id));
        await fixture.ViewModel.WhenIdleAsync();

        Assert.False(fixture.ViewModel.UnsavedPromptVisible);
        Assert.Equal(existing.Id, fixture.ViewModel.SelectedRejection!.Id);
    }

    private static IdeationRejection Rejection(Sample sample, string text, string? reason) =>
        new(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, text, reason, IdeationMode.Basic, Now);

    private static IReadOnlyList<ScopeOption> ScopeOptions(Sample sample) =>
    [
        new ScopeOption("Whole workspace", RejectedPhraseScope.WholeWorkspaceView),
        new ScopeOption($"{sample.Niche.Name}", RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id)),
        new ScopeOption($"{sample.Group.Name}", RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id))
    ];

    private static Fixture CreateFixture(WorkspaceSnapshot snapshot, DateTimeOffset? nextClock = null)
    {
        var repository = new InMemoryRepository(snapshot);
        var service = new RejectedPhraseManagementService(
            repository,
            idGenerator: Guid.NewGuid,
            clock: () => nextClock ?? Now);
        return new Fixture(new RejectedPhrasesViewModel(service), repository);
    }

    private sealed class Fixture(RejectedPhrasesViewModel viewModel, InMemoryRepository repository)
    {
        public RejectedPhrasesViewModel ViewModel { get; } = viewModel;

        public InMemoryRepository Repository { get; } = repository;
    }

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;

        public bool FailSave { get; set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (FailSave)
            {
                throw new InvalidOperationException("Simulated save failure.");
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        TopicGroup Group)
    {
        public static Sample Create()
        {
            var store = new Store(Guid.NewGuid(), "Dog Shop", null, false, Now, Now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, Now, Now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, Now, Now, "{}");
            return new Sample(
                new WorkspaceSnapshot([store], [niche], [group], [], [], [], [], [], []),
                store,
                niche,
                group);
        }
    }
}
