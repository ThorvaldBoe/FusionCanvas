using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Ideation;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.Ideation;

public sealed class RejectedPhrasesLauncherTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ManageRejectedPhrasesCommand_DisabledUntilScopeOpenedAndServiceAvailable()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess(), rejectedPhrases: new StubRejectedPhraseService());

        Assert.False(viewModel.CanManageRejectedPhrases);

        viewModel.Open(Scope);

        Assert.True(viewModel.CanManageRejectedPhrases);
    }

    [Fact]
    public void ManageRejectedPhrasesCommand_DisabledWhenServiceAbsent()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess());

        viewModel.Open(Scope);

        Assert.False(viewModel.CanManageRejectedPhrases);
    }

    [Fact]
    public void OpenRejectedPhrases_CreatesManagerAndDoesNotDisturbIdeationState()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess(), rejectedPhrases: new StubRejectedPhraseService());
        viewModel.Open(Scope);
        viewModel.Guidance = "Grumpy";
        viewModel.CountText = "7";

        viewModel.OpenRejectedPhrases();

        Assert.True(viewModel.IsRejectedPhrasesOpen);
        Assert.NotNull(viewModel.RejectedPhrases);
        Assert.Equal("Grumpy", viewModel.Guidance);
        Assert.Equal("7", viewModel.CountText);
        Assert.Empty(viewModel.Candidates);
        Assert.False(viewModel.CanGenerate);
    }

    [Fact]
    public async Task CompleteRejectedPhrases_ReopensGeneration()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess(), rejectedPhrases: new StubRejectedPhraseService());
        viewModel.Open(Scope);

        viewModel.OpenRejectedPhrases();
        await viewModel.CompleteRejectedPhrasesAsync();

        Assert.False(viewModel.IsRejectedPhrasesOpen);
        Assert.Null(viewModel.RejectedPhrases);
        Assert.True(viewModel.CanGenerate);
    }

    [Fact]
    public async Task StateMutated_RaisesWorkspaceChanged()
    {
        var snapshot = SampleSnapshot();
        var store = Assert.Single(snapshot.Stores);
        var niche = Assert.Single(snapshot.Niches);
        var group = Assert.Single(snapshot.Groups);
        var scope = new IdeationScope(
            store.Id, niche.Id, group.Id, "Dog Shop / Dogs / Pugs",
            new ItemTopicReference(WorkspaceEntityKind.Group, group.Id));
        var repository = new InMemoryRepository(snapshot);
        var service = new RejectedPhraseManagementService(repository, Guid.NewGuid, () => Now);
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess(), rejectedPhrases: service);
        viewModel.Open(scope);
        var raised = false;
        viewModel.WorkspaceChanged += (_, _) => raised = true;

        viewModel.OpenRejectedPhrases();
        var manager = viewModel.RejectedPhrases!;
        await manager.WhenIdleAsync();
        manager.NewCommand.Execute(null);
        await manager.WhenIdleAsync();
        manager.Phrase = "Talk to me about pugs";
        manager.SaveCommand.Execute(null);
        await manager.WhenIdleAsync();

        Assert.True(raised);
        Assert.Single(repository.Snapshot.IdeationRejections);
    }

    private static IdeationScope Scope { get; } = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Store / Dogs / Pugs",
        new ItemTopicReference(WorkspaceEntityKind.Group, Guid.NewGuid()));

    private static WorkspaceSnapshot SampleSnapshot()
    {
        var store = new Store(Guid.NewGuid(), "Dog Shop", null, false, Now, Now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, Now, Now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, Now, Now, "{}");
        return new WorkspaceSnapshot([store], [niche], [group], [], [], [], [], [], []);
    }

    private sealed class AvailableAccess : IIdeationAccessStatus
    {
        public event EventHandler? AvailabilityChanged { add { } remove { } }

        public IdeationAccessAvailability GetAvailability() => IdeationAccessAvailability.Available;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpService : IIdeationService
    {
        public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
            IdeationScopeResult.Available(new IdeationScope(
                Guid.NewGuid(), Guid.NewGuid(), null, "Scope", new ItemTopicReference(entityKind, entityId)));

        public Task<IdeationGenerationResult> GenerateAsync(
            IdeationGenerationRequest request,
            IProgress<IdeationGenerationProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdeationGenerationResult.Failure("Not implemented.", request.Count));

        public Task<IdeationDecisionResult> CreateAsync(
            IdeationScope scope, string candidateText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(false, "Not implemented.", WorkspaceSnapshot.Empty));

        public Task<IdeationDecisionResult> RejectAsync(
            IdeationScope scope, string candidateText, string? reason, IdeationMode mode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(false, "Not implemented.", WorkspaceSnapshot.Empty));
    }

    private sealed class StubRejectedPhraseService : IRejectedPhraseManagementService
    {
        public Task<RejectedPhraseManagementResult> InitializeAsync(
            RejectedPhraseScope scope, string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectedPhraseManagementResult.Success(RejectedPhraseManagementState.Empty(scope)));

        public Task<RejectedPhraseManagementResult> LoadAsync(
            RejectedPhraseScope scope, string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectedPhraseManagementResult.Success(RejectedPhraseManagementState.Empty(scope)));

        public Task<RejectedPhraseManagementResult> CreateAsync(
            RejectedPhraseCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectedPhraseManagementResult.Success(RejectedPhraseManagementState.Empty(request.Scope)));

        public Task<RejectedPhraseManagementResult> UpdateAsync(
            RejectedPhraseUpdateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectedPhraseManagementResult.Success(RejectedPhraseManagementState.Empty(request.Scope)));

        public Task<RejectedPhraseManagementResult> DeleteAsync(
            Guid id, RejectedPhraseScope scope, string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectedPhraseManagementResult.Success(RejectedPhraseManagementState.Empty(scope)));
    }

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
