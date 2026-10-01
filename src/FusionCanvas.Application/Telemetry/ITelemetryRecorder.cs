namespace FusionCanvas.Application.Telemetry;

public interface ITelemetryRecorder
{
    bool IsCaptureEnabled { get; }

    Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default);
}
