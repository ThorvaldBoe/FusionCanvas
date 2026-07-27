using System.IO.Compression;
using System.Text.Json;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Integration.Files;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Packages;

public sealed class ZipWorkspacePackageReader : IWorkspacePackageReader
{
    public const int CurrentFormatVersion = 1;

    public async Task<WorkspacePackageReadResult> ReadAsync(
        string packagePath,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        FileStream? packageStream = null;
        ZipArchive? archive = null;
        DirectoryInfo? temporaryDirectory = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            packageStream = new FileStream(
                Path.GetFullPath(packagePath),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
            ValidateArchivePaths(archive);

            var manifestEntry = archive.GetEntry("manifest.json");
            var databaseEntry = archive.GetEntry("workspace.db");
            if (manifestEntry is null || databaseEntry is null)
            {
                return WorkspacePackageReadResult.Failure("The selected file is not a readable FusionCanvas workspace package.");
            }

            WorkspacePackageManifest? manifest;
            await using (var manifestStream = manifestEntry.Open())
            {
                manifest = await JsonSerializer.DeserializeAsync<WorkspacePackageManifest>(
                    manifestStream,
                    WorkspacePackageJson.Options,
                    cancellationToken);
            }

            if (manifest is null)
            {
                return WorkspacePackageReadResult.Failure("The workspace package manifest is missing or invalid.");
            }

            if (manifest.FormatVersion > CurrentFormatVersion ||
                manifest.SchemaVersion > SqliteWorkspaceRepository.CurrentSchemaVersion)
            {
                return WorkspacePackageReadResult.Failure("This workspace package requires a newer FusionCanvas version.");
            }

            ValidateManifestPaths(manifest);
            temporaryDirectory = Directory.CreateTempSubdirectory("fusioncanvas-import-");
            var databasePath = Path.Combine(temporaryDirectory.FullName, "workspace.db");
            progress?.Report(new WorkspaceTransferProgress("Reading workspace data", 0, 1));
            await using (var databaseInput = databaseEntry.Open())
            await using (var databaseOutput = new FileStream(
                databasePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await databaseInput.CopyToAsync(databaseOutput, cancellationToken);
            }

            var snapshot = await new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false)
                .LoadAsync(cancellationToken);
            progress?.Report(new WorkspaceTransferProgress("Reading workspace data", 1, 1));
            if (snapshot.Workspaces.Count != 1 ||
                snapshot.Workspaces[0].Id != manifest.WorkspaceId)
            {
                return WorkspacePackageReadResult.Failure("The workspace package data does not match its manifest.");
            }

            var skippedUnsupported = new List<string>();
            var restorableFiles = new List<WorkspacePackageReadEntry>();
            foreach (var file in manifest.Files)
            {
                var normalizedPath = WorkspaceFileReference.Normalize(file.Path);
                var entry = archive.GetEntry($"files/{normalizedPath}");
                if (entry is null)
                {
                    continue;
                }

                if (!LocalWorkspaceFileStore.IsSupportedCreativeAssetPath(normalizedPath))
                {
                    skippedUnsupported.Add(normalizedPath);
                    continue;
                }

                restorableFiles.Add(new WorkspacePackageReadEntry(
                    normalizedPath,
                    file.Size,
                    _ => Task.FromResult(entry.Open())));
            }

            var session = new ZipWorkspacePackageReadSession(
                packageStream,
                archive,
                temporaryDirectory,
                manifest,
                snapshot,
                restorableFiles,
                skippedUnsupported);
            packageStream = null;
            archive = null;
            temporaryDirectory = null;
            return WorkspacePackageReadResult.Success(session);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException or SqliteException or ArgumentException or InvalidOperationException)
        {
            return WorkspacePackageReadResult.Failure(
                exception.Message.Contains("requires a newer FusionCanvas", StringComparison.OrdinalIgnoreCase)
                    ? "This workspace package requires a newer FusionCanvas version."
                    : "The selected file is not a readable FusionCanvas workspace package.");
        }
        finally
        {
            archive?.Dispose();
            packageStream?.Dispose();
            if (temporaryDirectory is not null)
            {
                TryDeleteDirectory(temporaryDirectory);
            }
        }
    }

    private static void ValidateArchivePaths(ZipArchive archive)
    {
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (path is "manifest.json" or "workspace.db")
            {
                continue;
            }

            if (!path.StartsWith("files/", StringComparison.Ordinal) || path.EndsWith('/'))
            {
                throw new InvalidDataException("The package contains an invalid entry.");
            }

            WorkspaceFileReference.Normalize(path["files/".Length..]);
        }
    }

    private static void ValidateManifestPaths(WorkspacePackageManifest manifest)
    {
        foreach (var file in manifest.Files)
        {
            WorkspaceFileReference.Normalize(file.Path);
        }

        foreach (var file in manifest.MissingFiles)
        {
            WorkspaceFileReference.Normalize(file);
        }
    }

    private static void TryDeleteDirectory(DirectoryInfo directory)
    {
        try
        {
            if (directory.Exists)
            {
                directory.Delete(recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class ZipWorkspacePackageReadSession(
        FileStream packageStream,
        ZipArchive archive,
        DirectoryInfo temporaryDirectory,
        WorkspacePackageManifest manifest,
        Domain.Workspace.WorkspaceSnapshot snapshot,
        IReadOnlyList<WorkspacePackageReadEntry> files,
        IReadOnlyList<string> skippedUnsupportedFiles) : IWorkspacePackageReadSession
    {
        public WorkspacePackageManifest Manifest { get; } = manifest;

        public Domain.Workspace.WorkspaceSnapshot Snapshot { get; } = snapshot;

        public IReadOnlyList<WorkspacePackageReadEntry> Files { get; } = files;

        public IReadOnlyList<string> SkippedUnsupportedFiles { get; } = skippedUnsupportedFiles;

        public ValueTask DisposeAsync()
        {
            archive.Dispose();
            packageStream.Dispose();
            TryDeleteDirectory(temporaryDirectory);
            return ValueTask.CompletedTask;
        }
    }
}
