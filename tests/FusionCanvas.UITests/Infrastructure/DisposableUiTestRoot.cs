namespace FusionCanvas.UITests.Infrastructure;

internal sealed class DisposableUiTestRoot : IDisposable
{
    private readonly string _rootPath;
    private bool _disposed;

    public DisposableUiTestRoot()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "FusionCanvas.UITests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
        DatabasePath = Path.Combine(_rootPath, "workspace.db");
        WorkspaceRootPath = Path.Combine(_rootPath, "workspace-files");
        SettingsPath = Path.Combine(_rootPath, "settings.json");
    }

    public string DatabasePath { get; }

    public string WorkspaceRootPath { get; }

    public string SettingsPath { get; }

    public string RootPath => _rootPath;

    public string CreateApplicationArguments() => string.Join(' ',
        "--fusioncanvas-workspace-db", Quote(DatabasePath),
        "--fusioncanvas-workspace-root", Quote(WorkspaceRootPath),
        "--fusioncanvas-settings-path", Quote(SettingsPath));

    public bool Contains(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!Contains(DatabasePath) || !Contains(WorkspaceRootPath) || !Contains(SettingsPath))
        {
            throw new InvalidOperationException($"Refusing to clean a path outside disposable UI test root '{_rootPath}'.");
        }

        try
        {
            Directory.Delete(_rootPath, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"UI test cleanup could not remove disposable root '{_rootPath}'. Retain this path for diagnostics.", exception);
        }
    }

    private static string Quote(string path) => $"\"{path.Replace("\"", "\\\"")}\"";
}
