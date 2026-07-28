using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.RejectedPhrases;

public sealed class RejectedPhraseManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Initialize_LoadsAllWorkspaceRejectionsAtWholeWorkspaceView()
    {
        var sample = Sample.Create();
        var nicheRejection = Rejection(sample, null, "Niche root phrase", "Too generic");
        var groupRejection = Rejection(sample, sample.Group.Id, "Group phrase", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [nicheRejection, groupRejection] };
        var (service, _) = NewService(snapshot);

        var result = await service.InitializeAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.State.AllRejections.Count);
        Assert.Equal(2, result.State.VisibleRejections.Count);
    }

    [Fact]
    public async Task Load_FiltersBySearchAcrossPhraseAndReason()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, null, "Talk to me about pugs", "Off-brand");
        var second = Rejection(sample, null, "Cat life", "Too generic");
        var snapshot = sample.Snapshot with { IdeationRejections = [first, second] };
        var (service, _) = NewService(snapshot);

        var result = await service.LoadAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            "off-brand",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var visible = Assert.Single(result.State.VisibleRejections);
        Assert.Equal("Talk to me about pugs", visible.Text);
    }

    [Fact]
    public async Task Load_FiltersByGroupScope()
    {
        var sample = Sample.Create();
        var inGroup = Rejection(sample, sample.Group.Id, "Group phrase", null);
        var nicheRoot = Rejection(sample, null, "Niche root phrase", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [inGroup, nicheRoot] };
        var (service, _) = NewService(snapshot);

        var result = await service.LoadAsync(
            RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id),
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var visible = Assert.Single(result.State.VisibleRejections);
        Assert.Equal("Group phrase", visible.Text);
    }

    [Fact]
    public async Task Load_FiltersByNicheScopeAcrossGroups()
    {
        var sample = Sample.Create();
        var inGroup = Rejection(sample, sample.Group.Id, "Group phrase", null);
        var nicheRoot = Rejection(sample, null, "Niche root phrase", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [inGroup, nicheRoot] };
        var (service, _) = NewService(snapshot);

        var result = await service.LoadAsync(
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.State.VisibleRejections.Count);
    }

    [Fact]
    public async Task Create_PersistsAtActiveScopeWithBasicModeAndSelectsCreatedRecord()
    {
        var sample = Sample.Create();
        var (service, repository) = NewService(sample.Snapshot);

        var result = await service.CreateAsync(
            new RejectedPhraseCreateRequest(
                "Talk to me about pugs",
                "Off-brand",
                RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id)),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AffectedSummary);
        Assert.Equal(IdeationMode.Basic, result.AffectedSummary!.Mode);
        Assert.Null(result.AffectedSummary.UpdatedAt);
        Assert.Equal(sample.Group.Id, result.AffectedSummary.GroupId);
        var persisted = Assert.Single(repository.Snapshot.IdeationRejections);
        Assert.Equal("Talk to me about pugs", persisted.Text);
        Assert.Equal("Off-brand", persisted.Reason);
    }

    [Fact]
    public async Task Create_RefusesAtWholeWorkspaceView()
    {
        var sample = Sample.Create();
        var (service, repository) = NewService(sample.Snapshot);

        var result = await service.CreateAsync(
            new RejectedPhraseCreateRequest(
                "Talk to me about pugs",
                null,
                RejectedPhraseScope.WholeWorkspaceView),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("single store and niche", result.Error);
        Assert.Empty(repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task Create_RefusesWithinScopeDuplicate()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, sample.Group.Id, "Talk to me about pugs", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [existing] };
        var (service, repository) = NewService(snapshot);

        var result = await service.CreateAsync(
            new RejectedPhraseCreateRequest(
                "TALK to me about pugs",
                null,
                RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id)),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Error);
        Assert.Single(repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task Create_AllowsSamePhraseInDifferentScope()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, null, "Talk to me about pugs", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [existing] };
        var (service, repository) = NewService(snapshot);

        var result = await service.CreateAsync(
            new RejectedPhraseCreateRequest(
                "Talk to me about pugs",
                null,
                RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id)),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, repository.Snapshot.IdeationRejections.Count);
    }

    [Fact]
    public async Task Update_PreservesIdentityScopeModeAndCreatedAtAndAdvancesUpdatedAt()
    {
        var sample = Sample.Create();
        var createdAt = Now;
        var existing = new IdeationRejection(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, sample.Group.Id, "Talk to me about pugs", null, IdeationMode.Snowclones, createdAt);
        var snapshot = sample.Snapshot with { IdeationRejections = [existing] };
        var (service, repository) = NewService(snapshot, nextClock: createdAt.AddMinutes(10));

        var result = await service.UpdateAsync(
            new RejectedPhraseUpdateRequest(
                existing.Id,
                "Talk to me about cats",
                "Better reason",
                RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id)),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var persisted = Assert.Single(repository.Snapshot.IdeationRejections);
        Assert.Equal(existing.Id, persisted.Id);
        Assert.Equal(existing.StoreId, persisted.StoreId);
        Assert.Equal(existing.NicheId, persisted.NicheId);
        Assert.Equal(existing.GroupId, persisted.GroupId);
        Assert.Equal(IdeationMode.Snowclones, persisted.Mode);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal(createdAt.AddMinutes(10), persisted.UpdatedAt);
        Assert.Equal("Talk to me about cats", persisted.Text);
        Assert.Equal("Better reason", persisted.Reason);
    }

    [Fact]
    public async Task Update_AdvancesUpdatedAtWhenOnlyReasonChanges()
    {
        var sample = Sample.Create();
        var createdAt = Now;
        var existing = new IdeationRejection(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Phrase", null, IdeationMode.Basic, createdAt);
        var snapshot = sample.Snapshot with { IdeationRejections = [existing] };
        var (service, repository) = NewService(snapshot, nextClock: createdAt.AddMinutes(5));

        var result = await service.UpdateAsync(
            new RejectedPhraseUpdateRequest(
                existing.Id,
                "Phrase",
                "New reason",
                RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id)),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var persisted = Assert.Single(repository.Snapshot.IdeationRejections);
        Assert.Equal("Phrase", persisted.Text);
        Assert.Equal("New reason", persisted.Reason);
        Assert.Equal(createdAt.AddMinutes(5), persisted.UpdatedAt);
    }

    [Fact]
    public async Task Update_RefusesWithinScopeCollision()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, null, "Talk to me about pugs", null);
        var second = Rejection(sample, null, "Talk to me about cats", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [first, second] };
        var (service, repository) = NewService(snapshot);

        var result = await service.UpdateAsync(
            new RejectedPhraseUpdateRequest(
                second.Id,
                "TALK to me about pugs",
                null,
                RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id)),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Error);
        var unchanged = Assert.Single(repository.Snapshot.IdeationRejections, r => r.Id == second.Id);
        Assert.Equal("Talk to me about cats", unchanged.Text);
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, null, "First", null);
        var second = Rejection(sample, null, "Second", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [first, second] };
        var (service, repository) = NewService(snapshot);

        var result = await service.DeleteAsync(
            first.Id,
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var remaining = Assert.Single(repository.Snapshot.IdeationRejections);
        Assert.Equal(second.Id, remaining.Id);
    }

    [Fact]
    public async Task Delete_OfLastRecordLeavesEmptyState()
    {
        var sample = Sample.Create();
        var only = Rejection(sample, null, "Only", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [only] };
        var (service, repository) = NewService(snapshot);

        var result = await service.DeleteAsync(
            only.Id,
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.State.VisibleRejections);
        Assert.Empty(repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task Save_FailureIsRecoverableAndPreservesLastConfirmedState()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, null, "Phrase", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [existing] };
        var (service, repository) = NewService(snapshot);
        repository.FailSave = true;

        var result = await service.CreateAsync(
            new RejectedPhraseCreateRequest(
                "New phrase",
                null,
                RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id)),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Unable to save", result.Error);
        Assert.Single(repository.Snapshot.IdeationRejections);
    }

    private static IdeationRejection Rejection(Sample sample, Guid? groupId, string text, string? reason) =>
        new(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, groupId, text, reason, IdeationMode.Basic, Now);

    private static (RejectedPhraseManagementService Service, InMemoryRepository Repository) NewService(
        WorkspaceSnapshot snapshot,
        DateTimeOffset? nextClock = null)
    {
        var repository = new InMemoryRepository(snapshot);
        var service = new RejectedPhraseManagementService(
            repository,
            idGenerator: Guid.NewGuid,
            clock: () => nextClock ?? Now);
        return (service, repository);
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
