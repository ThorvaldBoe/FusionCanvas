using FusionCanvas.Application.AI;
using FusionCanvas.Application.Niches;

namespace FusionCanvas.Application.Tests.Niches;

public sealed class NichePopulationServiceTests
{
    [Fact]
    public async Task PopulateAsync_UsesGeneralPurposeAndRequestsOnlySelectedFields()
    {
        var ai = new FakeAiTextGenerationService
        {
            Result = AiTextResult.Success(
                "{\"Description\":\"A focused niche\",\"Risks\":\"ignore\",\"ResearchNotes\":\"ignore\",\"Unknown\":\"ignore\"}",
                "general-model")
        };
        var service = new NichePopulationService(ai);

        var result = await service.PopulateAsync(
            new NichePopulationRequest(
                "Coffee lovers",
                [NichePopulationField.Description, NichePopulationField.VisualStyleGuidance]),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A focused niche", result.Suggestions[NichePopulationField.Description]);
        Assert.DoesNotContain(NichePopulationField.VisualStyleGuidance, result.Suggestions.Keys);
        Assert.Equal(AiRequestPurpose.General, ai.Request!.Purpose);
        Assert.Contains("Coffee lovers", ai.Request.Messages[1].Text, StringComparison.Ordinal);
        Assert.Contains("Description", ai.Request.Messages[1].Text, StringComparison.Ordinal);
        Assert.Contains("VisualStyleGuidance", ai.Request.Messages[1].Text, StringComparison.Ordinal);
        Assert.Contains("wearable, legible, scalable", ai.Request.Messages[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Risks\":", ai.Request.Messages[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("ResearchNotes\":", ai.Request.Messages[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PopulateAsync_IgnoresUnknownAndExcludedFields()
    {
        var ai = new FakeAiTextGenerationService
        {
            Result = AiTextResult.Success(
                "{\"Audience\":\"Coffee fans\",\"Risks\":\"unsafe\",\"ResearchNotes\":\"unverified\",\"Constraints\":12}",
                "general-model")
        };
        var service = new NichePopulationService(ai);

        var result = await service.PopulateAsync(
            new NichePopulationRequest("Coffee", [NichePopulationField.Audience, NichePopulationField.Constraints]),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Coffee fans", result.Suggestions[NichePopulationField.Audience]);
        Assert.Single(result.Suggestions);
    }

    [Theory]
    [InlineData("not json", NichePopulationFailureKind.InvalidProviderResponse)]
    [InlineData("{}", NichePopulationFailureKind.NoUsableSuggestions)]
    public async Task PopulateAsync_RejectsMalformedOrEmptyResponses(string text, NichePopulationFailureKind expectedKind)
    {
        var ai = new FakeAiTextGenerationService
        {
            Result = AiTextResult.Success(text, "general-model")
        };
        var service = new NichePopulationService(ai);

        var result = await service.PopulateAsync(
            new NichePopulationRequest("Coffee", [NichePopulationField.Description]),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedKind, result.FailureKind);
        Assert.NotEmpty(result.Message!);
    }

    [Fact]
    public async Task PopulateAsync_MapsProviderFailureToSafeGuidance()
    {
        var ai = new FakeAiTextGenerationService
        {
            Result = AiTextResult.Failure(AiTextFailureKind.Authentication, "secret-key=must-not-escape")
        };
        var service = new NichePopulationService(ai);

        var result = await service.PopulateAsync(
            new NichePopulationRequest("Coffee", [NichePopulationField.Description]),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(NichePopulationFailureKind.ProviderFailure, result.FailureKind);
        Assert.DoesNotContain("secret-key", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PopulateAsync_RejectsMissingNameOrFieldsWithoutAiCall()
    {
        var ai = new FakeAiTextGenerationService();
        var service = new NichePopulationService(ai);

        var missingName = await service.PopulateAsync(
            new NichePopulationRequest(" ", [NichePopulationField.Description]),
            TestContext.Current.CancellationToken);
        var missingFields = await service.PopulateAsync(
            new NichePopulationRequest("Coffee", []),
            TestContext.Current.CancellationToken);

        Assert.Equal(NichePopulationFailureKind.InvalidRequest, missingName.FailureKind);
        Assert.Equal(NichePopulationFailureKind.InvalidRequest, missingFields.FailureKind);
        Assert.Null(ai.Request);
    }

    private sealed class FakeAiTextGenerationService : IAiTextGenerationService
    {
        public AiTextRequest? Request { get; private set; }

        public AiTextResult Result { get; set; } = AiTextResult.Success("{}", "general-model");

        public Task<AiAvailabilityResult> GetAvailabilityAsync(AiRequestPurpose purpose, CancellationToken cancellationToken = default) =>
            Task.FromResult(AiAvailabilityResult.Ready);

        public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(Result);
        }
    }
}
