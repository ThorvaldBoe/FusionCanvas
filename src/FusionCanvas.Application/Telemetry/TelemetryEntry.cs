namespace FusionCanvas.Application.Telemetry;

public sealed record TelemetryEntry(
    Guid Id,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt,
    string Area,
    string Name,
    string Severity,
    string Outcome,
    string Message,
    string? MetadataJson,
    string? RequestBody,
    string? ResponseBody,
    string? RequestDetailsJson,
    string? ResponseDetailsJson,
    Guid? CorrelationId)
{
    public string FormatForDisplay()
    {
        var timestamp = OccurredAt.ToUniversalTime().ToString("O");
        var lines = new List<string>
        {
            $"[{timestamp}] [{Severity}] [{Area}] {Name} — {Outcome}: {Message}"
        };
        if (RequestDetailsJson is not null || RequestBody is not null)
        {
            lines.Add("Request:");
            if (RequestDetailsJson is not null) lines.Add(RequestDetailsJson);
            if (RequestBody is not null) lines.Add(RequestBody);
        }
        if (ResponseDetailsJson is not null || ResponseBody is not null)
        {
            lines.Add("Response:");
            if (ResponseDetailsJson is not null) lines.Add(ResponseDetailsJson);
            if (ResponseBody is not null) lines.Add(ResponseBody);
        }
        if (MetadataJson is not null) lines.Add($"Context: {MetadataJson}");
        return string.Join(Environment.NewLine, lines);
    }
}
