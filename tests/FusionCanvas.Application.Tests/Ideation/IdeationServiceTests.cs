using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Ideation;

public sealed class IdeationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Generate_GroupScopeUsesDirectActiveAndRejectedIdeasAndSanitizesMetadata()
    {
        var sample = Sample.Create();
        var direct = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Draft, "Direct idea");
        var rejected = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Rejected, "Rejected item");
        var archivedRejected = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Rejected, "Archived rejected item") with { IsArchived = true };
        var emptyIdea = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Draft, string.Empty) with { MetadataJson = "{}" };
        var childItem = NewItem(sample.Store.Id, sample.Niche.Id, sample.Child.Id, ItemStatus.Draft, "Child idea");
        var rootItem = NewItem(sample.Store.Id, sample.Niche.Id, null, ItemStatus.Draft, "Root idea");
        var rejection = new IdeationRejection(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, sample.Group.Id, "Rejected candidate", "Too common", IdeationMode.Basic, Now);
        var snapshot = sample.Snapshot with
        {
            Items = [direct, rejected, archivedRejected, emptyIdea, childItem, rootItem],
            IdeationRejections = [rejection]
        };
        var generator = new CapturingGenerator();
        var service = NewService(new InMemoryRepository(snapshot), generator);
        var scope = service.ResolveScope(snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;

        var result = await service.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, "Grumpy", 1),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var context = Assert.Single(generator.Contexts);
        Assert.Equal(["Direct idea"], context.ActiveIdeas);
        Assert.Contains(context.RejectedIdeas, idea => idea.Text == "Rejected item" && idea.Reason is null);
        Assert.Contains(context.RejectedIdeas, idea => idea.Text == "Archived rejected item" && idea.Reason is null);
        Assert.Contains(context.RejectedIdeas, idea => idea.Text == "Rejected candidate" && idea.Reason == "Too common");
        Assert.DoesNotContain("Child idea", context.ActiveIdeas);
        Assert.DoesNotContain("Root idea", context.ActiveIdeas);
        Assert.Equal("playful", context.Store.Metadata["brand"]);
        Assert.False(context.Store.Metadata.ContainsKey("api_key"));
        Assert.False(context.Store.Metadata.ContainsKey("inheritedFrom:brand"));
    }

    [Fact]
    public async Task Generate_NicheScopeIncludesRootAndDescendantIdeas()
    {
        var sample = Sample.Create();
        var root = NewItem(sample.Store.Id, sample.Niche.Id, null, ItemStatus.Draft, "Root idea");
        var group = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Published, "Group idea");
        var archived = NewItem(sample.Store.Id, sample.Niche.Id, sample.Group.Id, ItemStatus.Draft, "Archived idea") with { IsArchived = true };
        var snapshot = sample.Snapshot with { Items = [root, group, archived] };
        var generator = new CapturingGenerator();
        var service = NewService(new InMemoryRepository(snapshot), generator);
        var scope = service.ResolveScope(snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;

        await service.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 1),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["Root idea", "Group idea"], Assert.Single(generator.Contexts).ActiveIdeas);
    }

    [Fact]
    public async Task Generate_DeduplicatesAndReportsPartialFailure()
    {
        var sample = Sample.Create();
        var generator = new DelegateGenerator((_, index, _) => index switch
        {
            0 => Task.FromResult("  Same   idea "),
            1 => Task.FromResult("same idea"),
            2 => throw new InvalidOperationException("failure"),
            _ => Task.FromResult("Different idea")
        });
        var service = NewService(new InMemoryRepository(sample.Snapshot), generator);
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;

        var result = await service.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 4),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(["Same   idea", "Different idea"], result.Candidates.Select(candidate => candidate.Text));
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Generate_NeverExceedsFourConcurrentOperations()
    {
        var sample = Sample.Create();
        var generator = new ConcurrencyGenerator();
        var service = NewService(new InMemoryRepository(sample.Snapshot), generator);
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;

        var result = await service.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 12),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.InRange(generator.Peak, 1, IdeationService.MaximumConcurrency);
    }

    [Fact]
    public async Task Create_WritesFullIdeaAndUsesFirstSentenceInExactGroup()
    {
        var sample = Sample.Create();
        var repository = new InMemoryRepository(sample.Snapshot);
        var service = NewService(repository, new CapturingGenerator());
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;

        var result = await service.CreateAsync(
            scope,
            "A grumpy pug drinks coffee. A second supporting sentence.",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(sample.Group.Id, result.CreatedItem!.GroupId);
        Assert.Equal("A grumpy pug drinks coffee.", result.CreatedItem.Name);
        Assert.Contains("A second supporting sentence.", result.CreatedItem.MetadataJson);
        Assert.Equal(WorkflowStage.Idea, result.CreatedItem.Stage);
        Assert.Equal(ItemStatus.Draft, result.CreatedItem.Status);
    }

    [Fact]
    public async Task Create_UsesNicheRootWhenNoGroupIsSelected()
    {
        var sample = Sample.Create();
        var repository = new InMemoryRepository(sample.Snapshot);
        var service = NewService(repository, new CapturingGenerator());
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;

        var result = await service.CreateAsync(
            scope,
            "It's a pug's life",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(result.CreatedItem!.GroupId);
        Assert.Equal(sample.Niche.Id, result.CreatedItem.NicheId);
    }

    [Fact]
    public async Task Reject_PersistsReasonWithoutCreatingItem()
    {
        var sample = Sample.Create();
        var repository = new InMemoryRepository(sample.Snapshot);
        var rejectionId = Guid.NewGuid();
        var service = NewService(repository, new CapturingGenerator(), () => rejectionId);
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;

        var result = await service.RejectAsync(
            scope,
            "Weak phrase",
            "Too generic",
            IdeationMode.Snowclones,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.State.Items);
        var saved = Assert.Single(result.State.IdeationRejections);
        Assert.Equal(rejectionId, saved.Id);
        Assert.Null(saved.GroupId);
        Assert.Equal("Too generic", saved.Reason);
    }

    [Fact]
    public async Task Generate_IsBlockedWhenAccessIsUnavailable()
    {
        var sample = Sample.Create();
        var generator = new CapturingGenerator();
        var repository = new InMemoryRepository(sample.Snapshot);
        var itemManagement = new ItemManagementService(repository);
        var service = new IdeationService(
            repository,
            itemManagement,
            generator,
            new FixedCatalog(),
            new FixedAccess(false));
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;

        var result = await service.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 1),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(generator.Contexts);
    }

    [Theory]
    [InlineData(0, IdeationMode.Basic)]
    [InlineData(21, IdeationMode.Basic)]
    [InlineData(1, (IdeationMode)99)]
    public async Task Generate_RejectsInvalidCountOrMode(int count, IdeationMode mode)
    {
        var sample = Sample.Create();
        var generator = new CapturingGenerator();
        var service = NewService(new InMemoryRepository(sample.Snapshot), generator);
        var scope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;

        var result = await service.GenerateAsync(
            new IdeationGenerationRequest(scope, mode, null, count),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(generator.Contexts);
    }

    [Fact]
    public async Task Generate_TotalFailureAndCancellationReturnExplicitResults()
    {
        var sample = Sample.Create();
        var scope = new IdeationScopeResolver()
            .Resolve(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id).Scope!;
        var failing = NewService(
            new InMemoryRepository(sample.Snapshot),
            new DelegateGenerator((_, _, _) => throw new InvalidOperationException("failed")));

        var failed = await failing.GenerateAsync(
            new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 2),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(failed.Succeeded);
        Assert.Equal(2, failed.Failed);
        Assert.Empty(failed.Candidates);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await NewService(new InMemoryRepository(sample.Snapshot), new CapturingGenerator())
            .GenerateAsync(
                new IdeationGenerationRequest(scope, IdeationMode.Basic, null, 2),
                cancellationToken: cancellation.Token);

        Assert.True(cancelled.Cancelled);
        Assert.False(cancelled.Succeeded);
    }

    [Fact]
    public async Task Decisions_RevalidateStaleScopeAndRejectSaveIsAtomic()
    {
        var sample = Sample.Create();
        var staleRepository = new InMemoryRepository(sample.Snapshot);
        var staleService = NewService(staleRepository, new CapturingGenerator());
        var scope = staleService.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;
        staleRepository.Snapshot = sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups
                .Select(group => group.Id == sample.Group.Id ? group with { IsArchived = true } : group)
                .ToArray()
        };

        Assert.False((await staleService.CreateAsync(scope, "Candidate", TestContext.Current.CancellationToken)).Succeeded);
        Assert.False((await staleService.RejectAsync(scope, "Candidate", null, IdeationMode.Basic, TestContext.Current.CancellationToken)).Succeeded);

        var failingRepository = new InMemoryRepository(sample.Snapshot) { FailSave = true };
        var service = NewService(failingRepository, new CapturingGenerator());
        var activeScope = service.ResolveScope(sample.Snapshot, WorkspaceEntityKind.Group, sample.Group.Id).Scope!;
        var rejection = await service.RejectAsync(
            activeScope,
            "Candidate",
            null,
            IdeationMode.Basic,
            TestContext.Current.CancellationToken);

        Assert.False(rejection.Succeeded);
        Assert.Empty(failingRepository.Snapshot.IdeationRejections);
    }

    private static IdeationService NewService(
        InMemoryRepository repository,
        IIdeaGenerator generator,
        Func<Guid>? idGenerator = null)
    {
        var itemManagement = new ItemManagementService(
            repository,
            null,
            null,
            new GuidItemIdGenerator());
        return new IdeationService(
            repository,
            itemManagement,
            generator,
            new FixedCatalog(),
            new FixedAccess(true),
            idGenerator: idGenerator,
            clock: () => Now);
    }

    private static Item NewItem(Guid storeId, Guid nicheId, Guid? groupId, ItemStatus status, string idea) =>
        new(Guid.NewGuid(), storeId, nicheId, groupId, idea, null, status, WorkflowStage.Idea, false, Now, Now, $$"""{"idea":"{{idea}}"}""");

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;

        public bool FailSave { get; init; }

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

    private sealed class FixedAccess(bool available) : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() =>
            available ? IdeationAccessAvailability.Available : IdeationAccessAvailability.Unavailable("Unavailable");
    }

    private sealed class FixedCatalog : ISnowcloneCatalog
    {
        public IReadOnlyList<string> GetTemplates(int count) =>
            Enumerable.Range(0, count).Select(index => $"Template {index} X").ToArray();
    }

    private sealed class CapturingGenerator : IIdeaGenerator
    {
        public List<IdeationGenerationContext> Contexts { get; } = [];

        public Task<string> GenerateAsync(IdeationGenerationContext context, int requestIndex, CancellationToken cancellationToken = default)
        {
            lock (Contexts)
            {
                Contexts.Add(context);
            }

            return Task.FromResult($"Candidate {requestIndex}");
        }
    }

    private sealed class DelegateGenerator(
        Func<IdeationGenerationContext, int, CancellationToken, Task<string>> generate) : IIdeaGenerator
    {
        public Task<string> GenerateAsync(IdeationGenerationContext context, int requestIndex, CancellationToken cancellationToken = default) =>
            generate(context, requestIndex, cancellationToken);
    }

    private sealed class ConcurrencyGenerator : IIdeaGenerator
    {
        private int _active;
        private int _peak;

        public int Peak => _peak;

        public async Task<string> GenerateAsync(IdeationGenerationContext context, int requestIndex, CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(ref _active);
            UpdatePeak(active);
            try
            {
                await Task.Delay(5, cancellationToken);
                return $"Candidate {requestIndex}";
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        private void UpdatePeak(int value)
        {
            while (true)
            {
                var current = Volatile.Read(ref _peak);
                if (value <= current || Interlocked.CompareExchange(ref _peak, value, current) == current)
                {
                    return;
                }
            }
        }
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        TopicGroup Group,
        TopicGroup Child)
    {
        public static Sample Create()
        {
            var store = new Store(
                Guid.NewGuid(),
                "Dog Shop",
                "Funny shirts",
                false,
                Now,
                Now,
                """{"brand":"playful","api_key":"never","inheritedFrom:brand":"store"}""");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", "Dog owners", false, Now, Now, """{"humor":"dry"}""");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, Now, Now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, group.Id, "Coffee", null, false, Now, Now, "{}");
            return new Sample(
                new WorkspaceSnapshot([store], [niche], [group, child], [], [], [], [], [], []),
                store,
                niche,
                group,
                child);
        }
    }
}
