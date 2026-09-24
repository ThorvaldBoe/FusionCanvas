namespace FusionCanvas.Application.Telemetry;

public interface ITelemetryWorkspaceContext
{
    Guid? ActiveWorkspaceId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
}
