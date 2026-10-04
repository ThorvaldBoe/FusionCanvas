using FusionCanvas.Application.AI;
using FusionCanvas.Domain.ContentRisk;
using System.Text.Json;

namespace FusionCanvas.Application.ContentRisk;

public sealed class ConfiguredAiContentRiskAnalyzer : IContentRiskAnalyzer
{
    private const string AnalyzerId = "configured-ai-content-risk-v1";
    private readonly IAiTextGenerationService _ai;

    public ConfiguredAiContentRiskAnalyzer(IAiTextGenerationService ai)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
    }

    public async Task<ContentRiskAnalysisResult> AnalyzeAsync(
        ContentRiskAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var availability = await _ai.GetAvailabilityAsync(AiRequestPurpose.ContentRisk, cancellationToken).ConfigureAwait(false);
        if (!availability.IsReady || (request.Target.ContentKind == ContentRiskContentKind.Image && !availability.SupportsImageInput))
        {
            return ContentRiskAnalysisResult.Unavailable(
                ContentRiskAnalyzerFailureKind.NotConfigured,
                availability.Message);
        }

        var system = new AiTextMessage(
            AiMessageRole.System,
            "Review the supplied print-on-demand content for possible IP risk, harmful or inappropriate content, and marketplace suitability. Return only JSON in the form {\"findings\":[{\"category\":\"ip_risk|safety_risk|marketplace_suitability\",\"severity\":\"low|medium|high\",\"explanation\":\"short advisory explanation\",\"evidence\":\"short bounded evidence\"}]}. An empty findings array means no obvious signal was detected, not clearance. Do not provide legal conclusions.");
        var user = new AiTextMessage(
            AiMessageRole.User,
            request.Target.ContentKind == ContentRiskContentKind.Text
                ? $"Customer-facing role: {request.Target.Role}\nContent:\n{request.Text}"
                : $"Customer-facing role: {request.Target.Role}\nReview the attached final managed artwork image.",
            request.ImageBytes is null ? null : [new AiImageInput(request.MediaType!, request.ImageBytes)]);

        var result = await _ai.GenerateAsync(new AiTextRequest(AiRequestPurpose.ContentRisk, [system, user]), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Text))
        {
            return ContentRiskAnalysisResult.Unavailable(
                MapFailure(result.FailureKind),
                MapFailureMessage(result.FailureKind));
        }

        try
        {
            return ContentRiskAnalysisResult.Success(
                ContentRiskAnalyzerResponseCodec.Parse(result.Text),
                AnalyzerId,
                result.Usage?.TotalTokens,
                result.Usage?.Cost);
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or ArgumentException)
        {
            return ContentRiskAnalysisResult.Unavailable(
                ContentRiskAnalyzerFailureKind.InvalidResponse,
                "The content-risk review returned an invalid result.");
        }
    }

    private static ContentRiskAnalyzerFailureKind MapFailure(AiTextFailureKind? kind) => kind switch
    {
        AiTextFailureKind.NotConfigured or AiTextFailureKind.CredentialUnavailable or AiTextFailureKind.InvalidConfiguration => ContentRiskAnalyzerFailureKind.NotConfigured,
        AiTextFailureKind.InvalidProviderResponse or AiTextFailureKind.IncompleteGeneration => ContentRiskAnalyzerFailureKind.InvalidResponse,
        _ => ContentRiskAnalyzerFailureKind.ProviderFailure
    };

    private static string MapFailureMessage(AiTextFailureKind? kind) => kind switch
    {
        AiTextFailureKind.NotConfigured or AiTextFailureKind.CredentialUnavailable or AiTextFailureKind.InvalidConfiguration =>
            "Content-risk review is not configured.",
        AiTextFailureKind.InvalidProviderResponse or AiTextFailureKind.IncompleteGeneration =>
            "Content-risk review returned an invalid result.",
        _ => "Content-risk review could not be completed."
    };
}
