namespace FusionCanvas.Application.ContentRisk;

public interface IContentRiskAnalyzer
{
    Task<ContentRiskAnalysisResult> AnalyzeAsync(
        ContentRiskAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
