using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.Integration.Packages;

public sealed class ZipWorkspacePackageWriter : IWorkspacePackageWriter
{
    public int CurrentFormatVersion => ZipWorkspacePackageReader.CurrentFormatVersion;

    public int CurrentSchemaVersion => SqliteWorkspaceRepository.CurrentSchemaVersion;

    public string AppVersion => GetAppVersion();

    public async Task<WorkspacePackageWriteResult> WriteAsync(
        WorkspacePackageWriteRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var destinationPath = Path.GetFullPath(request.DestinationPath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The package destination directory is invalid.");
        Directory.CreateDirectory(destinationDirectory);

        var operationId = Guid.NewGuid().ToString("N");
        var temporaryPackagePath = Path.Combine(destinationDirectory, $".{Path.GetFileName(destinationPath)}.{operationId}.tmp");
        var temporaryDirectory = Directory.CreateTempSubdirectory($"fusioncanvas-export-{operationId}-");
        var databasePath = Path.Combine(temporaryDirectory.FullName, "workspace.db");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkspaceTransferProgress("Writing workspace data", 0, 1));
            await new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false)
                .SaveAsync(request.Snapshot, cancellationToken);
            progress?.Report(new WorkspaceTransferProgress("Writing workspace data", 1, 1));

            var packagedFiles = new List<WorkspacePackageFile>();
            var missingFiles = request.Manifest.MissingFiles.ToHashSet(StringComparer.Ordinal);
            var filePaths = request.Snapshot.Assets
                .Select(asset => WorkspaceFileReference.Normalize(asset.WorkspaceRelativePath))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            WorkspacePackageManifest finalManifest;
            await using (var output = new FileStream(
                temporaryPackagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                var databaseEntry = archive.CreateEntry("workspace.db", CompressionLevel.Optimal);
                await using (var databaseOutput = databaseEntry.Open())
                await using (var databaseInput = File.OpenRead(databasePath))
                {
                    await databaseInput.CopyToAsync(databaseOutput, cancellationToken);
                }

                for (var index = 0; index < filePaths.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = filePaths[index];
                    progress?.Report(new WorkspaceTransferProgress("Writing managed files", index, filePaths.Length));
                    if (!request.FileStore.Exists(relativePath))
                    {
                        missingFiles.Add(relativePath);
                        continue;
                    }

                    await using var source = await request.FileStore.OpenReadAsync(relativePath, cancellationToken);
                    var fileEntry = archive.CreateEntry($"files/{relativePath}", CompressionLevel.Optimal);
                    await using var destination = fileEntry.Open();
                    var size = await CopyAndCountAsync(source, destination, cancellationToken);
                    packagedFiles.Add(new WorkspacePackageFile(relativePath, size));
                }

                progress?.Report(new WorkspaceTransferProgress("Writing managed files", filePaths.Length, filePaths.Length));
                finalManifest = request.Manifest with
                {
                    AppVersion = string.IsNullOrWhiteSpace(request.Manifest.AppVersion)
                        ? GetAppVersion()
                        : request.Manifest.AppVersion,
                    Files = packagedFiles,
                    MissingFiles = missingFiles.Order(StringComparer.Ordinal).ToArray()
                };
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
                await using (var manifestStream = manifestEntry.Open())
                {
                    await JsonSerializer.SerializeAsync(
                        manifestStream,
                        finalManifest,
                        WorkspacePackageJson.Options,
                        cancellationToken);
                }

                await output.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPackagePath, destinationPath, overwrite: true);
            return new WorkspacePackageWriteResult(finalManifest);
        }
        finally
        {
            TryDeleteFile(temporaryPackagePath);
            try
            {
                temporaryDirectory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static async Task<long> CopyAndCountAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
        }

        return total;
    }

    private static string GetAppVersion() =>
        typeof(ZipWorkspacePackageWriter).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? typeof(ZipWorkspacePackageWriter).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
