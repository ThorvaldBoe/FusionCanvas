using System.Diagnostics;
using System.Text;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Integration.Updates;

public sealed class WindowsInstallerLauncher : IUpdateInstallerLauncher
{
    public Task ScheduleAfterExitAsync(string installerPath, int processId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("The verified update installer could not be found.", installerPath);
        }

        var script = $"$ErrorActionPreference='Stop'; $p=Get-Process -Id {processId} -ErrorAction SilentlyContinue; if ($null -ne $p) {{ $p.WaitForExit() }}; Start-Process -FilePath '{EscapePowerShellLiteral(installerPath)}'";
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {encodedCommand}",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? AppContext.BaseDirectory
        });

        if (process is null)
        {
            throw new InvalidOperationException("The update helper process could not be started.");
        }

        process.Dispose();
        return Task.CompletedTask;
    }

    private static string EscapePowerShellLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
