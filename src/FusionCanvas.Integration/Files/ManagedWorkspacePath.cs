namespace FusionCanvas.Integration.Files;

internal static class ManagedWorkspacePath
{
    public static string Normalize(string workspaceRelativePath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRelativePath))
        {
            throw new ArgumentException("Workspace-relative path must not be empty.", nameof(workspaceRelativePath));
        }

        var normalized = workspaceRelativePath.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(normalized))
        {
            throw new ArgumentException("Workspace-relative path must not be rooted.", nameof(workspaceRelativePath));
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Workspace-relative path must stay within the managed workspace.", nameof(workspaceRelativePath));
        }

        return string.Join('/', segments);
    }
}
