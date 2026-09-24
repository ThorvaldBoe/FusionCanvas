namespace FusionCanvas.Application.Telemetry;

public sealed record WorkspaceTelemetrySettings(
    bool DebugModeEnabled,
    TelemetryRetentionPeriod RetentionPeriod,
    bool ShowDebugWindow)
{
    public static WorkspaceTelemetrySettings Default { get; } =
        new(false, TelemetryRetentionPeriod.OneDay, false);
}
