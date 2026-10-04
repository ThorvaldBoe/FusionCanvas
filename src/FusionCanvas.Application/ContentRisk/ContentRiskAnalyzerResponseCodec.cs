using System.Text.Json;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public static class ContentRiskAnalyzerResponseCodec
{
    public const int MaximumFindings = 12;
    public const int MaximumResponseBytes = 32_000;

    public static IReadOnlyList<ContentRiskFinding> Parse(string response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (System.Text.Encoding.UTF8.GetByteCount(response) > MaximumResponseBytes)
        {
            throw new InvalidDataException("The content-risk analyzer response is too large.");
        }

        using var document = JsonDocument.Parse(response, new JsonDocumentOptions { MaxDepth = 8 });
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("findings", out var findingsElement) ||
            findingsElement.ValueKind != JsonValueKind.Array ||
            findingsElement.GetArrayLength() > MaximumFindings)
        {
            throw new InvalidDataException("The content-risk analyzer response has an invalid findings list.");
        }

        var findings = new List<ContentRiskFinding>();
        foreach (var element in findingsElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty("category", out var categoryElement) ||
                !element.TryGetProperty("severity", out var severityElement) ||
                !element.TryGetProperty("explanation", out var explanationElement) ||
                categoryElement.ValueKind != JsonValueKind.String ||
                severityElement.ValueKind != JsonValueKind.String ||
                explanationElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException("The content-risk analyzer returned an invalid finding.");
            }

            if (!TryParseCategory(categoryElement.GetString(), out var category) ||
                !TryParseSeverity(severityElement.GetString(), out var severity))
            {
                throw new InvalidDataException("The content-risk analyzer returned an unknown category or severity.");
            }

            var explanation = explanationElement.GetString();
            var evidence = element.TryGetProperty("evidence", out var evidenceElement) && evidenceElement.ValueKind == JsonValueKind.String
                ? evidenceElement.GetString()
                : null;
            findings.Add(new ContentRiskFinding(category, severity, explanation ?? string.Empty, evidence));
        }

        return findings;
    }

    private static bool TryParseCategory(string? value, out ContentRiskCategory category)
    {
        category = value?.Trim().ToLowerInvariant() switch
        {
            "ip" or "ip_risk" or "iprisk" => ContentRiskCategory.IpRisk,
            "safety" or "safety_risk" or "harmful" => ContentRiskCategory.SafetyRisk,
            "marketplace" or "marketplace_suitability" or "suitability" => ContentRiskCategory.MarketplaceSuitability,
            _ => default
        };
        return value?.Trim().ToLowerInvariant() is "ip" or "ip_risk" or "iprisk" or "safety" or "safety_risk" or "harmful" or "marketplace" or "marketplace_suitability" or "suitability";
    }

    private static bool TryParseSeverity(string? value, out ContentRiskSeverity severity)
    {
        severity = value?.Trim().ToLowerInvariant() switch
        {
            "low" => ContentRiskSeverity.Low,
            "medium" or "moderate" => ContentRiskSeverity.Medium,
            "high" => ContentRiskSeverity.High,
            _ => default
        };
        return value?.Trim().ToLowerInvariant() is "low" or "medium" or "moderate" or "high";
    }
}
