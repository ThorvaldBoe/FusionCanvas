using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.App.Settings;

public sealed class TelemetryWorkspaceContext : ITelemetryWorkspaceContext
{
    private readonly object _gate = new();
    private Guid? _activeWorkspaceId;

    public Guid? ActiveWorkspaceId
    {
        get
        {
            lock (_gate) return _activeWorkspaceId;
        }
    }

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        lock (_gate) _activeWorkspaceId = workspaceId;
    }
}
