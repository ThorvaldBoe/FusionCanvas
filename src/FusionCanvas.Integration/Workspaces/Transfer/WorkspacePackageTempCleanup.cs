using System.Diagnostics;

namespace FusionCanvas.Integration.Packages;

internal static class WorkspacePackageTempCleanup
{
    internal static void Cleanup(string temporaryPackagePath, DirectoryInfo temporaryDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPackagePath);
        ArgumentNullException.ThrowIfNull(temporaryDirectory);

        Cleanup(
            temporaryPackagePath,
            temporaryDirectory,
            File.Delete,
            directory => directory.Delete(recursive: true));
    }

    internal static void Cleanup(
        string temporaryPackagePath,
        DirectoryInfo temporaryDirectory,
        Action<string> deleteFile,
        Action<DirectoryInfo> deleteDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPackagePath);
        ArgumentNullException.ThrowIfNull(temporaryDirectory);
        ArgumentNullException.ThrowIfNull(deleteFile);
        ArgumentNullException.ThrowIfNull(deleteDirectory);

        TryDeleteFile(temporaryPackagePath, deleteFile);
        TryDeleteDirectory(temporaryDirectory, deleteDirectory);
    }

    private static void TryDeleteFile(string path, Action<string> deleteFile)
    {
        try
        {
            if (File.Exists(path))
            {
                deleteFile(path);
            }
        }
        catch (IOException exception)
        {
            Trace.TraceWarning(
                "Workspace package temporary file cleanup failed for '{0}': {1}",
                path,
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning(
                "Workspace package temporary file cleanup failed for '{0}': {1}",
                path,
                exception.Message);
        }
    }

    private static void TryDeleteDirectory(DirectoryInfo directory, Action<DirectoryInfo> deleteDirectory)
    {
        try
        {
            if (directory.Exists)
            {
                deleteDirectory(directory);
            }
        }
        catch (IOException exception)
        {
            Trace.TraceWarning(
                "Workspace package temporary directory cleanup failed for '{0}': {1}",
                directory.FullName,
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Trace.TraceWarning(
                "Workspace package temporary directory cleanup failed for '{0}': {1}",
                directory.FullName,
                exception.Message);
        }
    }
}
