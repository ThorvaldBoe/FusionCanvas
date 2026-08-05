using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.SllGeneration;

public sealed class SllGenerationServiceTests
{
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid NicheId = Guid.NewGuid();
    private static readonly Guid TagId = Guid.NewGuid();

    private const string SuppliedPhrase = "I'M NOT SAYING I'D SURVIVE A DRAGON ATTACK";

    private static string SampleResponse(string? phrase = null) =>
        $"""
        ASSUMPTIONS:
        - Unisex product assumed

        INTENT:
        wearer_signal: I am the cautious player
        viewer_inference: Recognizes the behavior
        emotion: dry humor
        shared_context: tabletop players

        TRIANGLE:
        idea: A cautious adventurer negotiates with a dragon
        phrase: {phrase ?? SuppliedPhrase}
        graphic: a dragon head leaning toward a speech bubble
        relationship: completion

        ASCII_SKETCH:
        ```text
        +----------------------------------+
        |    I'M NOT SAYING I'D SURVIVE    |
        |         A DRAGON ATTACK          |
        +----------------------------------+
        ```

        NOTES:
        composition: narrative stack
        typography: bold sans
        graphic_style: woodcut
        colors: warm bone
        texture_effects: none
        placement_scale: centered 28cm
        production: legible at thumb

        VALIDATION:
        reading_order: setup then payoff
        thumbnail: PHRASE anchor
        signal: clear wearer signal
        largest_risk: phrase length
        """;

    [Fact]
    public async Task GenerateAsync_WhenAiSucceeds_ParsesBlocksAndReturnsSuccess()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(SampleResponse(), "test-model");

        var result = await service.GenerateAsync(
            ItemId,
            Triangle(),
            SuppliedPhrase,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Document);
        Assert.Contains("Unisex product assumed", result.Document!.Assumptions);
        Assert.Equal("I am the cautious player", result.Document.Communication.WearerSignal);
        Assert.Equal(SuppliedPhrase, result.Document.Triangle.Phrase);
        Assert.Contains("I'M NOT SAYING I'D SURVIVE", result.Document.AsciiSketch);
        Assert.Equal("narrative stack", result.Document.Notes.Composition);
        Assert.Equal("phrase length", result.Document.Validation.LargestRisk);
    }

    [Fact]
    public async Task GenerateAsync_WhenAiFails_ReturnsFailure()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Failure(AiTextFailureKind.ProviderFailure, "Provider error.");

        var result = await service.GenerateAsync(ItemId, Triangle(), SuppliedPhrase, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Provider error.", result.Error);
    }

    [Fact]
    public async Task GenerateAsync_WhenResponseMissingBlock_ReturnsFailed()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success("ASSUMPTIONS:\n- one\n\nNO OTHER BLOCKS", "test-model");

        var result = await service.GenerateAsync(ItemId, Triangle(), SuppliedPhrase, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
    }

    [Fact]
    public async Task GenerateAsync_WhenPhraseMutatedUnlabeled_ReturnsFailed()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(SampleResponse(phrase: "A TOTALLY DIFFERENT PHRASE"), "test-model");

        var result = await service.GenerateAsync(ItemId, Triangle(), SuppliedPhrase, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
    }

    [Fact]
    public async Task GenerateAsync_WhenPhraseRevisedExplicitly_ReturnsSuccess()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        var revised = SampleResponse(phrase: "A SHORTER REWORDING") + "\nREVISED PHRASE: stated separately";
        var revisedResponse = revised.Replace("relationship: completion", "relationship: completion\nREVISED PHRASE: A SHORTER REWORDING is preferred", StringComparison.Ordinal);

        ai.Result = AiTextResult.Success(revisedResponse, "test-model");

        var result = await service.GenerateAsync(ItemId, Triangle(), SuppliedPhrase, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task GenerateAsync_WhenSketchEmpty_ReturnsFailed()
    {
        var emptySketch = SampleResponse()
            .Replace("+----------------------------------+", "", StringComparison.Ordinal)
            .Replace("|    I'M NOT SAYING I'D SURVIVE    |", "", StringComparison.Ordinal)
            .Replace("|         A DRAGON ATTACK          |", "", StringComparison.Ordinal);
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(emptySketch, "test-model");

        var result = await service.GenerateAsync(ItemId, Triangle(), SuppliedPhrase, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(AiTextFailureKind.InvalidProviderResponse, result.FailureKind);
    }

    [Fact]
    public async Task GenerateAsync_CapturedRequest_UsesSllPurposeAndContainsFrameworkAndContextAndNoOperationalFields()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(SampleResponse(), "test-model");

        await service.GenerateAsync(ItemId, Triangle(), "Negotiating with a dragon", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        Assert.Equal(AiRequestPurpose.Sll, ai.LastRequest.Purpose);
        Assert.Equal(2, ai.LastRequest.Messages.Count);

        var systemMessage = ai.LastRequest.Messages[0];
        var userMessage = ai.LastRequest.Messages[1];

        Assert.Equal(AiMessageRole.System, systemMessage.Role);
        Assert.Contains("Sketch Layout Language", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASSUMPTIONS", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASCII_SKETCH", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REVISED PHRASE", systemMessage.Text, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(AiMessageRole.User, userMessage.Role);
        Assert.Contains("Negotiating with a dragon", userMessage.Text);
        Assert.Contains(SuppliedPhrase, userMessage.Text);
        Assert.Contains("Test Store", userMessage.Text);
        Assert.Contains("Test Niche", userMessage.Text);
        Assert.Contains("test-tag", userMessage.Text);
        Assert.Contains("A test store", userMessage.Text);
        Assert.Contains("Topic: (none)", userMessage.Text);

        Assert.DoesNotContain(ItemId.ToString(), userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apikey", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdat", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_SystemMessageBindsUntrustedContent()
    {
        var (service, ai, _) = CreateService(CreateSnapshot());
        ai.Result = AiTextResult.Success(SampleResponse(), "test-model");

        await service.GenerateAsync(ItemId, Triangle(), "Negotiating with a dragon", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        var systemMessage = ai.LastRequest.Messages[0];

        Assert.Equal(AiMessageRole.System, systemMessage.Role);
        Assert.Contains("untrusted", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("instructions", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("output rules", systemMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_AdversarialMetadata_ExcludesOperationalKeys()
    {
        var snapshot = CreateAdversarialSnapshot();
        var (service, ai, _) = CreateService(snapshot);
        ai.Result = AiTextResult.Success(SampleResponse(), "test-model");

        await service.GenerateAsync(ItemId, Triangle(), "Base idea", TestContext.Current.CancellationToken);

        Assert.NotNull(ai.LastRequest);
        var userMessage = ai.LastRequest.Messages[1];

        Assert.Contains("brand=Adversarial", userMessage.Text);
        Assert.DoesNotContain("apikey", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", userMessage.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inherited", userMessage.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static ConceptRefinementTriangle Triangle() =>
        new("A cautious adventurer negotiates with a dragon", SuppliedPhrase, "a dragon head leaning toward a speech bubble");

    private static (SllGenerationService Service, CapturingAi Ai, InMemoryRepository Repo) CreateService(WorkspaceSnapshot snapshot)
    {
        var ai = new CapturingAi();
        var repo = new InMemoryRepository(snapshot);
        var guidance = new StubGuidanceSource();
        var service = new SllGenerationService(repo, ai, guidance);
        return (service, ai, repo);
    }

    private static WorkspaceSnapshot CreateAdversarialSnapshot()
    {
        var metadata = """{"brand":"Adversarial","apiKey":"sk-12345","path":"/secret/","dbPath":"C:\\db","token":"abc","credential":"pwd","secret":"hidden","createdAt":"2024-01-01","inheritedFrom":"parent-group","id":"x123","tone":"dark"}""";
        var store = new Store(StoreId, "Adv Store", "Adv store desc", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, metadata);
        var niche = new Niche(NicheId, StoreId, "Adv Niche", "Adv niche desc", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, metadata);
        var item = new Item(ItemId, StoreId, NicheId, null, "Test Item", null, ItemStatus.Draft, WorkflowStage.Concept, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var tag = new Tag(TagId, StoreId, "adv-tag", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var itemTag = new ItemTag(ItemId, TagId);

        return new WorkspaceSnapshot([store], [niche], [], [item], [], [], [tag], [itemTag], []);
    }

    private static WorkspaceSnapshot CreateSnapshot()
    {
        var store = new Store(StoreId, "Test Store", "A test store", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"theme": "nature"}""");
        var niche = new Niche(NicheId, StoreId, "Test Niche", "A test niche", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var item = new Item(ItemId, StoreId, NicheId, null, "Test Item", null, ItemStatus.Draft, WorkflowStage.Concept, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, """{"idea": "Negotiating with a dragon", "concept.idea": "A cozy cabin", "phrase": "Find peace", "graphicDirection": "Snowy trees"}""");
        var tag = new Tag(TagId, StoreId, "test-tag", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var itemTag = new ItemTag(ItemId, TagId);

        return new WorkspaceSnapshot([store], [niche], [], [item], [], [], [tag], [itemTag], []);
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
        public string Load() => "# Sketch Layout Language\n\nAn SLL is a compact human-readable specification of a complete PoD design with an ASCII sketch.";
    }
}
