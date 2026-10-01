using System.Diagnostics;
using System.IO;

namespace FusionCanvas.Application.Workspaces;

internal static class ManagedWorkspaceFileCleanup
{
    internal enum Status
    {
        Deleted,
        AlreadyMissing,
        Failed,
        Uninspectable
    }

    internal sealed record Result(
        Status Status,
        Exception? Failure = null);

    internal const string StatusDataKey = "FusionCanvas.ManagedWorkspaceFileCleanup.Status";
    private const string FailureDataKey = "FusionCanvas.ManagedWorkspaceFileCleanup.Failure";

    internal static Task<Result> TryDeleteAsync(IWorkspaceFileStore fileStore, string workspaceRelativePath) =>
        TryDeleteAsync(
            () => fileStore.TryDelete(workspaceRelativePath),
            cancellationToken => fileStore.OpenReadAsync(workspaceRelativePath, cancellationToken));

    internal static Task<Result> TryDeleteAsync(IWorkspaceFileOutputStore fileStore, string workspaceRelativePath) =>
        TryDeleteAsync(
            () => fileStore.TryDelete(workspaceRelativePath),
            cancellationToken => fileStore.OpenReadAsync(workspaceRelativePath, cancellationToken));

    private static async Task<Result> TryDeleteAsync(
        Func<bool> tryDelete,
        Func<CancellationToken, Task<Stream>> openRead)
    {
        Exception? deleteException = null;
        try
        {
            if (tryDelete())
            {
                return new(Status.Deleted);
            }
        }
        catch (Exception exception)
        {
            deleteException = exception;
        }

        try
        {
            await using var remainingFile = await openRead(CancellationToken.None).ConfigureAwait(false);
            return new(Status.Failed, deleteException);
        }
        catch (FileNotFoundException)
        {
            return new(Status.AlreadyMissing);
        }
        catch (DirectoryNotFoundException)
        {
            return new(Status.AlreadyMissing);
        }
        catch (Exception inspectionException)
        {
            return new(Status.Uninspectable, Combine(deleteException, inspectionException));
        }
    }

    internal static void PreserveDiagnostic(Exception primaryException, Result cleanup, string operation)
    {
        primaryException.Data[StatusDataKey] = cleanup.Status.ToString();
        if (cleanup.Failure is not null)
        {
            primaryException.Data[FailureDataKey] = cleanup.Failure;
        }

        if (cleanup.Status is Status.Failed or Status.Uninspectable)
        {
            try
            {
                Trace.TraceError("Managed workspace file cleanup for {0} ended with {1}.", operation, cleanup.Status);
            }
            catch (Exception)
            {
                // Preserve the original persistence error if diagnostic listeners fail.
            }
        }
    }

    internal static string FailureMessage(Result cleanup) => cleanup.Status switch
    {
        Status.Failed => " The managed file could not be removed and may remain orphaned.",
        Status.Uninspectable => " The managed file removal could not be verified.",
        _ => string.Empty
    };

    private static Exception? Combine(Exception? deleteException, Exception inspectionException) =>
        deleteException is null ? inspectionException : new AggregateException(deleteException, inspectionException);
}
