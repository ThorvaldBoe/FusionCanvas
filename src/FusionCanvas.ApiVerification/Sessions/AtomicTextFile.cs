namespace FusionCanvas.ApiVerification.Sessions;

internal static class AtomicTextFile
{
    public static async Task WriteAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A parent directory is required.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, cancellationToken).ConfigureAwait(false);
            try
            {
                if (File.Exists(fullPath))
                    File.Replace(temporaryPath, fullPath, destinationBackupFileName: null);
                else
                    File.Move(temporaryPath, fullPath);
            }
            catch (IOException)
            {
                await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
