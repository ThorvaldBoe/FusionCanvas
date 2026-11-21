using System.Net.Http;
using System.Text.Json;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Application.Updates;

/// <summary>Creates bounded, best-effort diagnostic events for the update flow.</summary>
public static class UpdateTelemetry
{
    public static Task RecordCheckFailureAsync(
        ITelemetryRecorder? recorder,
        Exception exception,
        string? currentVersion,
        UpdateTrack track) =>
        RecordAsync(recorder, "UpdateCheckFailed", "Failed", "check", exception, "currentVersion", currentVersion, track);

    public static Task RecordDownloadFailureAsync(
        ITelemetryRecorder? recorder,
        Exception exception,
        string stage,
        UpdateManifest manifest) =>
        RecordAsync(recorder, "UpdateDownloadFailed", "Failed", stage, exception, "targetVersion", manifest.ProductVersion, manifest.Track);

    public static Task RecordDownloadSucceededAsync(ITelemetryRecorder? recorder, UpdateManifest manifest)
    {
        if (recorder is null || !IsEnabled(recorder))
        {
            return Task.CompletedTask;
        }

        return TryRecordAsync(recorder, new TelemetryEventRequest(
            Area: "Updates",
            Name: "UpdateDownloadSucceeded",
            Severity: "Information",
            Outcome: "Succeeded",
            Message: $"Verified update package is ready (version {manifest.ProductVersion}, track {manifest.Track}).",
            MetadataJson: JsonSerializer.Serialize(new
            {
                targetVersion = manifest.ProductVersion,
                track = manifest.Track.ToString()
            })));
    }

    private static Task RecordAsync(
        ITelemetryRecorder? recorder,
        string name,
        string outcome,
        string stage,
        Exception exception,
        string versionField,
        string? targetVersion,
        UpdateTrack track)
    {
        if (recorder is null || !IsEnabled(recorder))
        {
            return Task.CompletedTask;
        }

        var statusCode = FindHttpStatus(exception);
        var metadata = new Dictionary<string, object?>
        {
            ["stage"] = stage,
            [versionField] = targetVersion,
            ["track"] = track.ToString(),
            ["exceptionType"] = exception.GetType().Name,
            ["hResult"] = exception.HResult,
            ["httpStatus"] = statusCode is null ? null : (int?)statusCode.Value
        };
        var safeSummary = statusCode is null
            ? $"{exception.GetType().Name} (HRESULT 0x{exception.HResult:X8})"
            : $"{exception.GetType().Name} (HTTP {(int)statusCode.Value}, HRESULT 0x{exception.HResult:X8})";

        return TryRecordAsync(recorder, new TelemetryEventRequest(
            Area: "Updates",
            Name: name,
            Severity: "Error",
            Outcome: outcome,
            Message: $"Update {stage} failed: {safeSummary}.",
            MetadataJson: JsonSerializer.Serialize(metadata)));
    }

    private static bool IsEnabled(ITelemetryRecorder? recorder)
    {
        try
        {
            return recorder?.IsCaptureEnabled == true;
        }
        catch
        {
            return false;
        }
    }

    private static System.Net.HttpStatusCode? FindHttpStatus(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: { } statusCode })
            {
                return statusCode;
            }
        }

        return null;
    }

    private static async Task TryRecordAsync(ITelemetryRecorder recorder, TelemetryEventRequest request)
    {
        try
        {
            await recorder.RecordAsync(request).ConfigureAwait(false);
        }
        catch
        {
            // Diagnostics must never change update availability, verification, or installation behavior.
        }
    }
}
