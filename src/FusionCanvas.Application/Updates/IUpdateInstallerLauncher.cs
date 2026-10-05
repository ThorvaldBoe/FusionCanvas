namespace FusionCanvas.Application.Updates;

public interface IUpdateInstallerLauncher
{
    Task ScheduleAfterExitAsync(string installerPath, int processId, CancellationToken cancellationToken = default);
}
