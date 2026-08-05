using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.TitleOptimization;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.TitleOptimization;

public sealed class TitleOptimizationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Optimize_FirstCandidateUnique_MakesSingleCallAndReturnsIt()
    {
        var sample = Sample.Create();
        var ai = new ScriptedAi(["Pug coach hostage"]);
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Pug coach hostage", result.Title);
        Assert.Equal(1, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_CollisionPromptsDistinguishingWord_AndStopsWhenUnique()
    {
        var sample = Sample.Create();
        var sibling = NewItem(sample.Store.Id, sample.Niche.Id, "Pug coach hostage");
        var snapshot = sample.Snapshot with { Items = [sample.Item, sibling] };
        var ai = new ScriptedAi(["Pug coach hostage", "Pug coach hostage mug"]);
        var service = new TitleOptimizationService(new InMemoryRepository(snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Pug coach hostage mug", result.Title);
        Assert.Equal(2, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_BoundedLoopAppliesNumericSuffixWhenCollisionPersists()
    {
        var sample = Sample.Create();
        var sibling = NewItem(sample.Store.Id, sample.Niche.Id, "Pug coach hostage");
        var snapshot = sample.Snapshot with { Items = [sample.Item, sibling] };
        var ai = new ScriptedAi(["Pug coach hostage"]); // always collides
        var service = new TitleOptimizationService(new InMemoryRepository(snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Pug coach hostage 2", result.Title);
        Assert.Equal(TitleUniquenessPolicy.MaximumAttempts, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_IdenticalDataReachesBoundAndStillAppliesNumericSuffix()
    {
        var sample = Sample.Create();
        var twin = NewItem(sample.Store.Id, sample.Niche.Id, "Pug coach hostage");
        var snapshot = sample.Snapshot with { Items = [sample.Item, twin] };
        var ai = new ScriptedAi(["Pug coach hostage"]);
        var service = new TitleOptimizationService(new InMemoryRepository(snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Pug coach hostage 2", result.Title);
    }

    [Fact]
    public async Task Optimize_NoContent_ReturnsFailureWithoutAnyAiCall()
    {
        var sample = Sample.Create(creativeContent: false);
        var ai = new ScriptedAi(["Should not be called"]);
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.Title);
        Assert.Equal(0, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_ExcludesOperationalAndSecretMetadataFromPrompt()
    {
        var sample = Sample.Create();
        var ai = new ScriptedAi(["Pug coach hostage"]);
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        var request = Assert.Single(ai.Calls);
        var combined = string.Join(" ", request.Messages.Select(message => message.Text));
        Assert.DoesNotContain("S3CR3T_val_99", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("api_key", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden_cred", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Optimize_ArchivedItem_ReturnsFailureWithoutAiCall()
    {
        var sample = Sample.Create(withArchivedItem: true);
        var ai = new ScriptedAi(["Should not be called"]);
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(0, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_AiFailureBeforeAcceptance_ReturnsFailureWithoutTitle()
    {
        var sample = Sample.Create();
        var ai = new ScriptedAi([null]); // fails on first call
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.Title);
        Assert.Equal(1, ai.Calls.Count);
    }

    [Fact]
    public async Task Optimize_MultiLineResult_NormalizedToOneLine()
    {
        var sample = Sample.Create();
        var ai = new ScriptedAi(["Pug\n coach \nhostage"]);
        var service = new TitleOptimizationService(new InMemoryRepository(sample.Snapshot), ai);

        var result = await service.OptimizeAsync(
            new TitleOptimizationRequest(sample.Item.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Pug coach hostage", result.Title);
        Assert.False(result.Title!.Contains('\n'));
    }

    private static Item NewItem(Guid storeId, Guid nicheId, string? name) =>
        new(Guid.NewGuid(), storeId, nicheId, null, name ?? "Untitled", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshotToSave, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ScriptedAi(IReadOnlyList<string?> script) : IAiTextGenerationService
    {
        public List<AiTextRequest> Calls { get; } = [];

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiAvailabilityResult.Ready);

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(request);
            var index = Math.Min(Calls.Count - 1, script.Count - 1);
            var text = script[index];
            return Task.FromResult(text is null
                ? AiTextResult.Failure(AiTextFailureKind.ProviderFailure, "Simulated provider failure.")
                : AiTextResult.Success(text, requestedModel: "model"));
        }
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        Item Item)
    {
        public static Sample Create(bool creativeContent = true, bool withArchivedItem = false, string? ideaOverride = null)
        {
            var store = new Store(
                Guid.NewGuid(),
                "Dog Shop",
                "Funny shirts",
                false,
                Now,
                Now,
                """{"brand":"playful","api_key":"S3CR3T_val_99","credential":"hidden_cred","inheritedFrom:brand":"store"}""");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", "Dog owners", false, Now, Now, """{"humor":"dry"}""");
            var idea = ideaOverride ?? "A pug owner framing a stubborn walk as tactical hostage negotiation.";
            var metadata = creativeContent
                ? $$"""{"idea":"{{idea}}"}"""
                : "{}";
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Working title here", null, ItemStatus.Draft, WorkflowStage.Idea, withArchivedItem, Now, Now, metadata);
            return new Sample(new WorkspaceSnapshot([store], [niche], [], [item], [], [], [], [], []), store, niche, item);
        }
    }
}
