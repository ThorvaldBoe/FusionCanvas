using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.ConceptRefinement;

public sealed class ConceptRefinementServiceTests
{
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid NicheId = Guid.NewGuid();
    private static readonly Guid TagId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    [Fact]
    public async Task InitializeAsync_WhenAiSucceeds_ParsesLabeledResponseAndReturnsSuccess()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(
            """
            IDEA: A cozy mountain cabin in winter
            PHRASE: Find your peace
            GRAPHIC: Snowy pine trees with warm cabin lights
            """,
            "test-model");

        var result = await service.InitializeAsync(ItemId, "Mountain cabin", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A cozy mountain cabin in winter", result.ConceptIdea);
        Assert.Equal("Find your peace", result.Phrase);
        Assert.Equal("Snowy pine trees with warm cabin lights", result.GraphicDirection);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task InitializeAsync_WhenAiFails_ReturnsFailure()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Failure(AiTextFailureKind.ProviderFailure, "Provider error.");

        var result = await service.InitializeAsync(ItemId, "Mountain cabin", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Provider error.", result.Error);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponseMalformed_ReturnsFailure()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("Some random text without labels", "test-model");

        var result = await service.InitializeAsync(ItemId, "Mountain cabin", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
        Assert.Null(result.ConceptIdea);
        Assert.Null(result.Phrase);
        Assert.Null(result.GraphicDirection);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponsePartial_ReturnsFailure()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(
            """
            IDEA: Only an idea
            PHRASE: 
            GRAPHIC: 
            """,
            "test-model");

        var result = await service.InitializeAsync(ItemId, "Mountain cabin", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
    }

    [Fact]
    public async Task InitializeAsync_CapturedRequest_ContainsGuidanceAndCreativeContextAndNoOperationalFields()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(
            """
            IDEA: A cozy mountain cabin in winter
            PHRASE: Find your peace
            GRAPHIC: Snowy pine trees with warm cabin lights
            """,
            "test-model");

        await service.InitializeAsync(ItemId, "Mountain cabin", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        Assert.Equal(AiRequestPurpose.Concept, ai.LastRequest.Purpose);
        Assert.Equal(2, ai.LastRequest.Messages.Count);

        var systemMessage = ai.LastRequest.Messages[0];
        var userMessage = ai.LastRequest.Messages[1];

        Assert.Equal(AiMessageRole.System, systemMessage.Role);
        Assert.Contains("design triangle", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("idea", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phrase", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("graphic", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IDEA:", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PHRASE:", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GRAPHIC:", systemMessage.Text, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(AiMessageRole.User, userMessage.Role);
        Assert.Contains("Mountain cabin", userMessage.Text);
        Assert.Contains("Test Store", userMessage.Text);
        Assert.Contains("Test Niche", userMessage.Text);
        Assert.Contains("test-tag", userMessage.Text);

        // VR-004: Store/niche descriptions present
        Assert.Contains("A test store", userMessage.Text);
        Assert.Contains("A test niche", userMessage.Text);

        // VR-004: Topic shows (none) for ungrouped item
        Assert.Contains("Topic: (none)", userMessage.Text);

        // No operational or secret fields
        Assert.DoesNotContain(ItemId.ToString(), userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credentials", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apikey", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdat", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefineAsync_FineTuneConceptIdea_ExtractsValue()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("A sun-drenched coastal village", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.FineTune,
            ConceptRefinementCorner.ConceptIdea,
            new ConceptRefinementTriangle("A cozy cabin", "Find peace", "Snowy trees"),
            "Mountain cabin",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A sun-drenched coastal village", result.ConceptIdea);
        Assert.Null(result.Phrase);
        Assert.Null(result.GraphicDirection);
    }

    [Fact]
    public async Task RefineAsync_FineTunePhrase_NormalizesToSingleLine()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("Live\r\nevery\r\nmoment", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.FineTune,
            ConceptRefinementCorner.Phrase,
            new ConceptRefinementTriangle("A cozy cabin", "Find peace", "Snowy trees"),
            "Mountain cabin",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Live every moment", result.Phrase);
        Assert.Null(result.ConceptIdea);
        Assert.Null(result.GraphicDirection);
    }

    [Fact]
    public async Task RefineAsync_ChangeGraphicDirection_ExtractsValue()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("Abstract watercolor splashes", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.Change,
            ConceptRefinementCorner.GraphicDirection,
            new ConceptRefinementTriangle("A cozy cabin", "Find peace", ""),
            "Mountain cabin",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Abstract watercolor splashes", result.GraphicDirection);
    }

    [Fact]
    public async Task RefineAsync_ResponseWithLabelPrefix_StripsLabel()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("IDEA: A completely new direction", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.Change,
            ConceptRefinementCorner.ConceptIdea,
            new ConceptRefinementTriangle("Old idea", "Old phrase", "Old graphic"),
            "Original idea",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A completely new direction", result.ConceptIdea);
    }

    [Fact]
    public async Task RefineAsync_ResponseWithQuotes_StripsQuotes()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("\"A quoted response\"", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.Change,
            ConceptRefinementCorner.ConceptIdea,
            new ConceptRefinementTriangle("Old idea", "Old phrase", "Old graphic"),
            "Original idea",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A quoted response", result.ConceptIdea);
    }

    [Fact]
    public async Task RefineAsync_EmptyResponse_ReturnsFailure()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("   ", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.Change,
            ConceptRefinementCorner.ConceptIdea,
            new ConceptRefinementTriangle("Old idea", "Old phrase", "Old graphic"),
            "Original idea",
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
    }

    [Fact]
    public async Task RefineAsync_EmptyCornerForChange_Allowed()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("Fresh graphic concept", "test-model");

        var result = await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.Change,
            ConceptRefinementCorner.GraphicDirection,
            new ConceptRefinementTriangle("Idea", "Phrase", ""),
            "Original idea",
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Fresh graphic concept", result.GraphicDirection);
    }

    [Fact]
    public async Task RefineAsync_CapturedRequest_ContainsGuidanceAndTriangleAndNoOperationalData()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("Improved idea", "test-model");

        await service.RefineAsync(
            ItemId,
            ConceptRefinementActionKind.FineTune,
            ConceptRefinementCorner.ConceptIdea,
            new ConceptRefinementTriangle("Old idea", "Old phrase", "Old graphic"),
            "Original idea",
            TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        var userMessage = ai.LastRequest.Messages[1];
        Assert.Equal(AiMessageRole.User, userMessage.Role);
        Assert.Contains("Improve the Concept idea", userMessage.Text);
        Assert.Contains("Old idea", userMessage.Text);
        Assert.Contains("Old phrase", userMessage.Text);
        Assert.Contains("Old graphic", userMessage.Text);
        Assert.Contains("Original idea", userMessage.Text);
        Assert.Contains("Test Store", userMessage.Text);

        // VR-004: descriptions present
        Assert.Contains("A test store", userMessage.Text);
        Assert.Contains("A test niche", userMessage.Text);
        Assert.Contains("Topic: (none)", userMessage.Text);

        // No operational/secret fields
        Assert.DoesNotContain(ItemId.ToString(), userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apikey", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_WithGroupedItem_IncludesTopicNameAndMetadata()
    {
        var (service, ai, _) = CreateService(CreateGroupedSnapshot());
        ai.Result = AiTextResult.Success(
            """
            IDEA: Grouped concept idea
            PHRASE: Grouped phrase
            GRAPHIC: Grouped graphic
            """,
            "test-model");

        await service.InitializeAsync(ItemId, "Base idea", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        var userMessage = ai.LastRequest.Messages[1];
        Assert.Equal(AiMessageRole.User, userMessage.Role);

        // VR-004: Topic group name included
        Assert.Contains("Test Group", userMessage.Text);

        // VR-004: Group metadata included (sanitized - brand/style kept)
        Assert.Contains("groupstyle", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_AdversarialMetadata_ExcludesOperationalKeys()
    {
        // VR-005: store/niche/group metadata with id/path/apikey/token/createdat/inheritedFrom keys
        var snapshot = CreateAdversarialSnapshot();
        var (service, ai, _) = CreateService(snapshot);
        ai.Result = AiTextResult.Success(
            """
            IDEA: Clean idea
            PHRASE: Clean phrase
            GRAPHIC: Clean graphic
            """,
            "test-model");

        await service.InitializeAsync(ItemId, "Base idea", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        var userMessage = ai.LastRequest.Messages[1];
        Assert.Equal(AiMessageRole.User, userMessage.Role);

        // Sanitized values that should remain
        Assert.Contains("brand=Adversarial", userMessage.Text);
        Assert.Contains("tone=dark", userMessage.Text);

        // Operational keys that must be absent
        Assert.DoesNotContain("apikey", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inherited", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static (ConceptRefinementService Service, CapturingAi Ai, InMemoryRepository Repo) CreateService(WorkspaceSnapshot snapshot)
    {
        var ai = new CapturingAi();
        var repo = new InMemoryRepository(snapshot);
        var guidance = new StubGuidanceSource();
        var service = new ConceptRefinementService(repo, ai, guidance);
        return (service, ai, repo);
    }

    private static WorkspaceSnapshot CreateGroupedSnapshot()
    {
        var store = new Store(StoreId, "Test Store", "A test store", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"brand": "nature"}""");
        var niche = new Niche(NicheId, StoreId, "Test Niche", "A test niche", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var group = new TopicGroup(GroupId, StoreId, NicheId, null, "Test Group", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"groupstyle": "playful"}""");
        var item = new Item(ItemId, StoreId, NicheId, GroupId, "Test Item", null, ItemStatus.Draft, WorkflowStage.Concept, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"idea": "Base idea"}""");
        var tag = new Tag(TagId, StoreId, "test-tag", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var itemTag = new ItemTag(ItemId, TagId);

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [group],
            [item],
            [],
            [],
            [tag],
            [itemTag],
            []);
    }

    private static WorkspaceSnapshot CreateAdversarialSnapshot()
    {
        var metadata = """{"brand":"Adversarial","apiKey":"sk-12345","path":"/secret/","dbPath":"C:\\db","token":"abc","credential":"pwd","secret":"hidden","createdAt":"2024-01-01","inheritedFrom":"parent-group","id":"x123","tone":"dark"}""";
        var store = new Store(StoreId, "Adv Store", "Adv store desc", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, metadata);
        var niche = new Niche(NicheId, StoreId, "Adv Niche", "Adv niche desc", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, metadata);
        var item = new Item(ItemId, StoreId, NicheId, null, "Test Item", null, ItemStatus.Draft, WorkflowStage.Concept, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var tag = new Tag(TagId, StoreId, "adv-tag", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var itemTag = new ItemTag(ItemId, TagId);

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [],
            [item],
            [],
            [],
            [tag],
            [itemTag],
            []);
    }

    private static WorkspaceSnapshot CreateSnapshot()
    {
        var store = new Store(StoreId, "Test Store", "A test store", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"theme": "nature"}""");
        var niche = new Niche(NicheId, StoreId, "Test Niche", "A test niche", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var item = new Item(ItemId, StoreId, NicheId, null, "Test Item", null, ItemStatus.Draft, WorkflowStage.Concept, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"idea": "Mountain cabin", "concept.idea": "A cozy cabin", "phrase": "Find peace", "graphicDirection": "Snowy trees"}""");
        var tag = new Tag(TagId, StoreId, "test-tag", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var itemTag = new ItemTag(ItemId, TagId);
        var groups = Array.Empty<TopicGroup>();

        return new WorkspaceSnapshot(
            [store],
            [niche],
            groups,
            [item],
            [],
            [],
            [tag],
            [itemTag],
            []);
    }

    private sealed class CapturingAi : IAiTextGenerationService
    {
        public AiTextResult Result { get; set; } = AiTextResult.Success("default", "model");
        public AiTextRequest? LastRequest { get; private set; }
        public AiAvailabilityResult Availability { get; set; } = AiAvailabilityResult.Ready;

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Availability);

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubGuidanceSource : IDesignTriangleGuidanceSource
    {
        public string Load() => "# Design Triangle\n\nIdea = core emotion. Phrase = optional text. Graphic = optional visuals.";
    }
}