using System.Text.Json;
using System.Text.Json.Serialization;
using FusionCanvas.ApiVerification.Inventory;

namespace FusionCanvas.ApiVerification.Sessions;

internal sealed class VerificationReportWriter(string dataDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task WriteAsync(VerificationSessionReport report, CancellationToken cancellationToken = default)
    {
        var results = report.Operations.ToList();
        var completedOperationIds = results.Select(result => result.OperationId).ToHashSet(StringComparer.Ordinal);
        results.AddRange(ApiOperationInventory.Operations
            .Where(operation => operation.Provider.Equals(report.Provider, StringComparison.OrdinalIgnoreCase) && !completedOperationIds.Contains(operation.Id))
            .Select(operation => new VerificationOperationResult(operation.Id, "not-run", $"Not exercised by scenario `{report.Scenario}`.")));
        report = report with
        {
            Provider = SafeDiagnostic.Redact(report.Provider),
            Scenario = SafeDiagnostic.Redact(report.Scenario),
            Status = SafeDiagnostic.Redact(report.Status),
            Operations = results.Select(operation => operation with
            {
                OperationId = SafeDiagnostic.Redact(operation.OperationId),
                Status = SafeDiagnostic.Redact(operation.Status),
                Summary = operation.Summary is null ? null : SafeDiagnostic.Redact(operation.Summary),
                Diagnostic = operation.Diagnostic is null ? null : SafeDiagnostic.Redact(operation.Diagnostic),
                ModelId = operation.ModelId is null ? null : SafeDiagnostic.Redact(operation.ModelId),
                ProfileFingerprint = operation.ProfileFingerprint is null ? null : SafeDiagnostic.Redact(operation.ProfileFingerprint),
                ResourceId = operation.ResourceId is null ? null : SafeDiagnostic.Redact(operation.ResourceId)
            }).ToArray(),
            Findings = report.Findings.Select(finding => finding with
            {
                OperationId = SafeDiagnostic.Redact(finding.OperationId),
                Status = SafeDiagnostic.Redact(finding.Status),
                Concern = SafeDiagnostic.Redact(finding.Concern),
                Expected = SafeDiagnostic.Redact(finding.Expected),
                Actual = SafeDiagnostic.Redact(finding.Actual),
                ProductionLocation = finding.ProductionLocation is null ? null : SafeDiagnostic.Redact(finding.ProductionLocation)
            }).ToArray(),
            CleanupActions = report.CleanupActions.Select(SafeDiagnostic.Redact).ToArray()
        };
        Directory.CreateDirectory(dataDirectory);
        var fileName = $"{report.StartedAt:yyyyMMdd-HHmmss}-{Safe(report.Provider)}-{Safe(report.Scenario)}-{Safe(report.RunId)}";
        var jsonPath = Path.Combine(dataDirectory, fileName + ".json");
        var markdownPath = Path.Combine(dataDirectory, fileName + ".md");
        var json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(jsonPath, json, cancellationToken).ConfigureAwait(false);
        var markdown = RenderMarkdown(report);
        await File.WriteAllTextAsync(markdownPath, markdown, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Report: {jsonPath}");
    }

    private static string RenderMarkdown(VerificationSessionReport report)
    {
        var lines = new List<string>
        {
            $"# API Verification — {report.Provider} / {report.Scenario}",
            string.Empty,
            $"- Run: `{report.RunId}`",
            $"- Status: **{report.Status}**",
            $"- Started: {report.StartedAt:O}",
            $"- Completed: {report.CompletedAt:O}",
            $"- Estimated maximum cost: {report.EstimatedMaximumCostUsd?.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"} USD",
            $"- Actual reported cost: {report.ActualCostUsd?.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"} USD",
            string.Empty,
            "## Operations",
            string.Empty
        };

        foreach (var operation in report.Operations)
        {
            lines.Add($"- `{operation.OperationId}` — **{operation.Status}**{(operation.Summary is null ? string.Empty : $": {SafeText(operation.Summary)}")}");
            if (operation.Diagnostic is not null)
            {
                lines.Add($"  - Diagnostic: {SafeText(operation.Diagnostic)}");
            }
        }

        if (report.Findings.Count > 0)
        {
            lines.AddRange([string.Empty, "## Findings", string.Empty]);
            foreach (var finding in report.Findings)
            {
                lines.Add($"- `{finding.Id}` ({finding.Status}, `{finding.OperationId}`): {SafeText(finding.Concern)}; expected {SafeText(finding.Expected)}; observed {SafeText(finding.Actual)}");
            }
        }

        lines.AddRange([string.Empty, "## Cleanup", string.Empty]);
        lines.AddRange(report.CleanupActions.Select(action => $"- {SafeText(action)}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Safe(string value) => string.Concat(value.Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
    private static string SafeText(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
