using System.Text;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.App.IssueReporting;

internal static class ReportDiagnosticsFormatter
{
    public static string Format(TelemetryReportBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var output = new StringBuilder("FusionCanvas diagnostic report (recent events; review before sharing)");
        foreach (var entry in batch.Entries.OrderBy(value => value.OccurredAt))
        {
            var safe = TelemetrySecretRedactor.Sanitize(new TelemetryEventRequest(
                entry.Area, entry.Name, entry.Severity, entry.Outcome, entry.Message));
            output.AppendLine();
            output.Append('[').Append(entry.OccurredAt.ToUniversalTime().ToString("O"))
                .Append("] [").Append(safe.Severity).Append("] [").Append(safe.Area).Append("] ")
                .Append(safe.Name).Append(" — ").Append(safe.Outcome).Append(": ").Append(safe.Message);
        }

        if (batch.IsTruncated)
        {
            output.AppendLine().AppendLine("Additional events were omitted because the report reached its size limit.");
        }
        return output.ToString();
    }
}
