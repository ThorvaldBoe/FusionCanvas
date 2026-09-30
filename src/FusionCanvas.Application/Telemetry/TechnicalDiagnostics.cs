using System.Diagnostics;

namespace FusionCanvas.Application.Telemetry;

public static class TechnicalDiagnostics
{
    public static void RecordFailure(string operation, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(exception);

        Trace.TraceError(
            "FusionCanvas operation '{0}' failed with exception type '{1}'.",
            operation,
            exception.GetType().FullName ?? exception.GetType().Name);
    }
}
