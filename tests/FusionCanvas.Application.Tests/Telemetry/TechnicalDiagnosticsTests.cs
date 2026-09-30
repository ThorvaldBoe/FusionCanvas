using System.Diagnostics;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Application.Tests.Telemetry;

public sealed class TechnicalDiagnosticsTests
{
    [Fact]
    public void RecordFailureWritesOnlySafeTechnicalIdentity()
    {
        var messages = new List<string>();
        using var listener = new RecordingTraceListener(messages);
        Trace.Listeners.Add(listener);

        try
        {
            TechnicalDiagnostics.RecordFailure(
                "credential read",
                new InvalidOperationException("secret-token=must-not-be-recorded"));
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        var message = Assert.Single(messages.Where(message => message.Contains("FusionCanvas operation", StringComparison.Ordinal)));
        Assert.Contains("credential read", message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", message, StringComparison.Ordinal);
        Assert.DoesNotContain("must-not-be-recorded", message, StringComparison.Ordinal);
    }

    private sealed class RecordingTraceListener(List<string> messages) : TraceListener
    {
        public override void Write(string? message)
        {
            if (message is not null) messages.Add(message);
        }

        public override void WriteLine(string? message)
        {
            if (message is not null) messages.Add(message);
        }
    }
}
