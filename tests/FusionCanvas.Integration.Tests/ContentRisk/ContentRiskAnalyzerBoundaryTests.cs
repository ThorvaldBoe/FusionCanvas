using FusionCanvas.Application.AI;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Integration.Tests.ContentRisk;

public sealed class ContentRiskAnalyzerBoundaryTests
{
    [Fact]
    public async Task ConfiguredBoundaryUsesReviewPurposeAndSanitizesProviderFailure()
    {
        var ai = new FakeAi(AiTextResult.Failure(AiTextFailureKind.NetworkFailure, "secret provider payload"));
        var analyzer = new ConfiguredAiContentRiskAnalyzer(ai);
        var target = new ContentRiskReviewTarget(Guid.NewGuid(), ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");

        var result = await analyzer.AnalyzeAsync(new ContentRiskAnalysisRequest(target, text: "A title"));

        Assert.False(result.Succeeded);
        Assert.Equal(ContentRiskAnalyzerFailureKind.ProviderFailure, result.FailureKind);
        Assert.DoesNotContain("secret provider payload", result.Message);
        Assert.Equal(AiRequestPurpose.ContentRisk, ai.LastRequest!.Purpose);
    }

    [Fact]
    public void BoundaryRejectsOversizedImageBeforeProviderCall()
    {
        var target = new ContentRiskReviewTarget(Guid.NewGuid(), ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");

        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRiskAnalysisRequest(
            target,
            mediaType: "image/png",
            imageBytes: new byte[ContentRiskAnalysisRequest.MaximumImageBytes + 1]));
    }

    private sealed class FakeAi(AiTextResult result) : IAiTextGenerationService
    {
        public AiTextRequest? LastRequest { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(AiRequestPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(AiAvailabilityResult.Ready);

        public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
