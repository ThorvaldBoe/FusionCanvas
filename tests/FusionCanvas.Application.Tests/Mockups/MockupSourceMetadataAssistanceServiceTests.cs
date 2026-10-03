using FusionCanvas.Application.AI;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class MockupSourceMetadataAssistanceServiceTests
{
    [Fact]
    public async Task AssistAsync_UsesFilenameWithoutImageAndFallsBackToAllSizes()
    {
        var color = Guid.NewGuid();
        var small = Guid.NewGuid();
        var large = Guid.NewGuid();
        var ai = new FakeAi(
            new(AiAvailabilityKind.Ready, "ready", true),
            "{\"items\":[{\"token\":\"black.png\",\"valueIds\":[\"" + color + "\"],\"confidence\":0.95}]}" );
        var reader = new FakeImageReader();
        var service = new MockupSourceMetadataAssistanceService(ai, reader);

        var result = await service.AssistAsync(
            Request(
                new("black.png", "black.png", "black.png", 100, 100, [], null),
                [new(color, "Black", OptionKind.Color), new(small, "Small", OptionKind.Size), new(large, "Large", OptionKind.Size)]),
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.True(item.Applied);
        Assert.Contains(color, item.OptionValueIds);
        Assert.Contains(small, item.OptionValueIds);
        Assert.Contains(large, item.OptionValueIds);
        Assert.Empty(reader.Paths);
        Assert.All(ai.LastRequest!.Messages, message => Assert.Empty(message.Images));
    }

    [Fact]
    public async Task AssistAsync_AttachesOnlyImagesThatNeedVisualInference()
    {
        var ai = new FakeAi(
            new(AiAvailabilityKind.Ready, "ready", true),
            "{\"items\":[{\"token\":\"unknown.png\",\"valueIds\":[],\"confidence\":0.2}]}" );
        var reader = new FakeImageReader();
        var service = new MockupSourceMetadataAssistanceService(ai, reader);

        await service.AssistAsync(
            Request(new("unknown.png", "unknown.png", "unknown.png", 100, 100, [], null), []),
            TestContext.Current.CancellationToken);

        Assert.Equal(["unknown.png"], reader.Paths);
        Assert.Single(ai.LastRequest!.Messages.SelectMany(message => message.Images));
    }

    [Fact]
    public async Task AssistAsync_PreservesExistingValuesWhenConfidenceIsLowAndReusesUniquePlacement()
    {
        var color = Guid.NewGuid();
        var size = Guid.NewGuid();
        var mapping = new MockupImageSpaceMapping(100, 100, 5, 6, 70, 80);
        var ai = new FakeAi(
            new(AiAvailabilityKind.Ready, "ready", true),
            "{\"items\":[{\"token\":\"red.png\",\"valueIds\":[],\"confidence\":0.2}]}" );
        var service = new MockupSourceMetadataAssistanceService(ai, new FakeImageReader());

        var result = await service.AssistAsync(
            Request(
                new("red.png", "red.png", "red.png", 100, 100, [color, size], null),
                [new(color, "Red", OptionKind.Color), new(size, "Medium", OptionKind.Size)],
                [new("reference", [color, size], mapping)]),
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal([color, size], item.OptionValueIds);
        Assert.Equal(mapping, item.Mapping);
        Assert.Equal("Applied with review", item.Status);
    }

    [Fact]
    public async Task AssistAsync_ReportsUnavailableAiWithoutReadingOrGenerating()
    {
        var ai = new FakeAi(new(AiAvailabilityKind.MissingCredential, "Add an API key.", false), "{}");
        var reader = new FakeImageReader();
        var service = new MockupSourceMetadataAssistanceService(ai, reader);

        var result = await service.AssistAsync(Request(new("unknown.png", "unknown.png", "unknown.png", 1, 1, [], null), []), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(reader.Paths);
        Assert.False(ai.WasGenerated);
        Assert.Equal("Needs review", Assert.Single(result.Items).Status);
    }

    [Fact]
    public async Task AssistAsync_RejectsMalformedAndUnknownSuggestionsWithoutLosingKnownValues()
    {
        var color = Guid.NewGuid();
        var size = Guid.NewGuid();
        var ai = new FakeAi(
            new(AiAvailabilityKind.Ready, "ready", true),
            "not json");
        var service = new MockupSourceMetadataAssistanceService(ai, new FakeImageReader());

        var result = await service.AssistAsync(
            Request(
                new("existing.png", "existing.png", "existing.png", 100, 100, [color, size], null),
                [new(color, "Black", OptionKind.Color), new(size, "M", OptionKind.Size)]),
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.True(item.Applied);
        Assert.Equal([color, size], item.OptionValueIds);
        Assert.Equal("Applied with review", item.Status);
    }

    [Fact]
    public async Task AssistAsync_PreservesMappingWhenPlacementReferencesConflict()
    {
        var color = Guid.NewGuid();
        var existingMapping = new MockupImageSpaceMapping(100, 100, 1, 2, 50, 60);
        var firstMapping = new MockupImageSpaceMapping(100, 100, 3, 4, 50, 60);
        var secondMapping = new MockupImageSpaceMapping(100, 100, 5, 6, 50, 60);
        var ai = new FakeAi(
            new(AiAvailabilityKind.Ready, "ready", true),
            "{\"items\":[{\"token\":\"target.png\",\"valueIds\":[\"" + color + "\"],\"confidence\":0.95}]}" );
        var service = new MockupSourceMetadataAssistanceService(ai, new FakeImageReader());

        var result = await service.AssistAsync(
            Request(
                new("target.png", "target.png", "target.png", 100, 100, [], existingMapping),
                [new(color, "Black", OptionKind.Color)],
                [
                    new("reference-a", [color], firstMapping),
                    new("reference-b", [color], secondMapping)
                ]),
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(existingMapping, item.Mapping);
        Assert.Equal("Applied with review", item.Status);
        Assert.Contains("Placement", item.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssistAsync_HonorsCancellationBeforeReadingOrGenerating()
    {
        var ai = new FakeAi(new(AiAvailabilityKind.Ready, "ready", true), "{}");
        var reader = new FakeImageReader();
        var service = new MockupSourceMetadataAssistanceService(ai, reader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AssistAsync(
            Request(new("unknown.png", "unknown.png", "unknown.png", 1, 1, [], null), []),
            new CancellationToken(canceled: true)));

        Assert.Empty(reader.Paths);
        Assert.False(ai.WasGenerated);
    }

    private static MockupSourceMetadataAssistanceRequest Request(
        MockupSourceMetadataImage image,
        IReadOnlyList<MockupSourceMetadataValue> values,
        IReadOnlyList<MockupSourceMetadataPlacementReference>? references = null) =>
        new([image], values, references ?? []);

    private sealed class FakeAi(AiAvailabilityResult availability, string response) : IAiTextGenerationService
    {
        public AiTextRequest? LastRequest { get; private set; }
        public bool WasGenerated { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(AiRequestPurpose purpose, CancellationToken cancellationToken = default) => Task.FromResult(availability);

        public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken = default)
        {
            WasGenerated = true;
            LastRequest = request;
            return Task.FromResult(AiTextResult.Success(response, "model"));
        }
    }

    private sealed class FakeImageReader : IMockupSourceImageContentReader
    {
        public List<string> Paths { get; } = [];

        public Task<MockupSourceImageContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            Paths.Add(sourcePath);
            return Task.FromResult(new MockupSourceImageContent("image/png", [1, 2, 3]));
        }
    }
}
