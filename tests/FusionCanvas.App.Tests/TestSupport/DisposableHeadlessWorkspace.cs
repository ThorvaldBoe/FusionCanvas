using FusionCanvas.Application.Workspaces;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.App.Tests.TestSupport;

/// <summary>Owns all files used by a persistent headless scenario.</summary>
internal sealed class DisposableHeadlessWorkspace : IDisposable
{
    private readonly string _rootPath;
    private bool _disposed;

    internal DisposableHeadlessWorkspace()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "FusionCanvas", "headless", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
        DatabasePath = Path.Combine(_rootPath, "workspace.db");
        WorkspacePath = Path.Combine(_rootPath, "workspace");
        SettingsPath = Path.Combine(_rootPath, "settings.json");
        Directory.CreateDirectory(WorkspacePath);
    }

    internal string DatabasePath { get; }

    internal string RootPath => _rootPath;

    internal string WorkspacePath { get; }

    internal string SettingsPath { get; }

    internal IWorkspaceRepository CreateRepository() =>
        new SqliteWorkspaceRepository(DatabasePath, useConnectionPooling: false);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (!IsOwnedPath(_rootPath))
            throw new InvalidOperationException($"Refusing to clean up an unowned headless workspace path: {_rootPath}");

        try
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }
        catch (Exception exception)
        {
            throw new IOException($"Unable to clean up disposable headless workspace '{_rootPath}'. Retained path for diagnosis.", exception);
        }
    }

    private bool IsOwnedPath(string path) =>
        Path.GetFullPath(path).StartsWith(Path.Combine(Path.GetTempPath(), "FusionCanvas", "headless") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Path.GetFullPath(path), Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase);
}
