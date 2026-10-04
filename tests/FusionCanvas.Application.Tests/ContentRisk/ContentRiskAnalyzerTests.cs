using FusionCanvas.Application.AI;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.Tests.ContentRisk;

public sealed class ContentRiskAnalyzerTests
{
    private static readonly Guid OwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void CodecParsesSeparateRiskCategories()
    {
        var findings = ContentRiskAnalyzerResponseCodec.Parse("""
            {"findings":[
              {"category":"ip_risk","severity":"medium","explanation":"Possible brand reference.","evidence":"brand-like wording"},
              {"category":"safety_risk","severity":"high","explanation":"Possible violent content."}
            ]}
            """);

        Assert.Collection(
            findings,
            first => Assert.Equal(ContentRiskCategory.IpRisk, first.Category),
            second => Assert.Equal(ContentRiskCategory.SafetyRisk, second.Category));
    }

    [Fact]
    public void CodecRejectsUnknownCategoryAndOversizedFindings()
    {
        Assert.Throws<InvalidDataException>(() => ContentRiskAnalyzerResponseCodec.Parse("{\"findings\":[{\"category\":\"legal_clearance\",\"severity\":\"low\",\"explanation\":\"no\"}]}"));

        var findings = string.Join(',', Enumerable.Range(0, ContentRiskAnalyzerResponseCodec.MaximumFindings + 1)
            .Select(_ => "{\"category\":\"ip_risk\",\"severity\":\"low\",\"explanation\":\"possible\"}"));
        Assert.Throws<InvalidDataException>(() => ContentRiskAnalyzerResponseCodec.Parse($"{{\"findings\":[{findings}]}}"));
    }

    [Fact]
    public async Task ConfiguredAnalyzerMapsNoFindingsToSuccessfulAdvisoryResult()
    {
        var ai = new FakeAiTextGenerationService(AiTextResult.Success(
            "{\"findings\":[]}",
            "review-model",
            actualModel: "review-model",
            provider: "fake",
            usage: new AiTextUsage(4, 3, 7, 0.01m)));
        var analyzer = new ConfiguredAiContentRiskAnalyzer(ai);
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "ideation.idea");

        var result = await analyzer.AnalyzeAsync(new ContentRiskAnalysisRequest(target, text: "A coffee idea"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Findings);
        Assert.Equal("configured-ai-content-risk-v1", result.AnalyzerId);
        Assert.Equal(7, result.InputUnits);
        Assert.DoesNotContain(OwnerId.ToString(), Assert.Single(ai.Requests).Messages[1].Text);
    }

    [Fact]
    public async Task ConfiguredAnalyzerMapsProviderFailureToUnavailable()
    {
        var ai = new FakeAiTextGenerationService(AiTextResult.Failure(AiTextFailureKind.NetworkFailure, "provider failed"));
        var analyzer = new ConfiguredAiContentRiskAnalyzer(ai);
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");

        var result = await analyzer.AnalyzeAsync(new ContentRiskAnalysisRequest(target, text: "A title"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ContentRiskAnalyzerFailureKind.ProviderFailure, result.FailureKind);
        Assert.DoesNotContain("provider failed", result.Message);
    }

    [Fact]
    public async Task ConfiguredAnalyzerSendsBoundedImageWithoutPathOrOwnerIdentifiers()
    {
        var ai = new FakeAiTextGenerationService(AiTextResult.Success(
            "{\"findings\":[{\"category\":\"safety_risk\",\"severity\":\"low\",\"explanation\":\"Review image context.\"}]}",
            "review-model"), new AiAvailabilityResult(AiAvailabilityKind.Ready, "AI is ready.", SupportsImageInput: true));
        var analyzer = new ConfiguredAiContentRiskAnalyzer(ai);
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");

        var result = await analyzer.AnalyzeAsync(
            new ContentRiskAnalysisRequest(target, mediaType: "image/png", imageBytes: [1, 2, 3]),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var request = Assert.Single(ai.Requests);
        Assert.Equal(AiRequestPurpose.ContentRisk, request.Purpose);
        Assert.DoesNotContain(OwnerId.ToString(), request.Messages[1].Text);
        Assert.DoesNotContain("C:\\", request.Messages[1].Text);
        Assert.Equal([1, 2, 3], request.Messages[1].Images!.Single().Bytes);
    }

    private sealed class FakeAiTextGenerationService(
        AiTextResult result,
        AiAvailabilityResult? availability = null) : IAiTextGenerationService
    {
        public List<AiTextRequest> Requests { get; } = [];

        public Task<AiAvailabilityResult> GetAvailabilityAsync(AiRequestPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(availability ?? AiAvailabilityResult.Ready);

        public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }
}
