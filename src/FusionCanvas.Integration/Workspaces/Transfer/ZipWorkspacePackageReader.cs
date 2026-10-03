using System.IO.Compression;
using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Integration.Files;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Packages;

public sealed class ZipWorkspacePackageReader : IWorkspacePackageReader
{
    private readonly Func<string, IWorkspaceRepository> _repositoryFactory;

    public const int CurrentFormatVersion = 1;

    /// <summary>Maximum size of the compressed package on disk.</summary>
    public const long MaxPackageBytes = 256L * 1024 * 1024;

    /// <summary>Maximum uncompressed size of the JSON manifest.</summary>
    public const long MaxManifestBytes = 1L * 1024 * 1024;

    /// <summary>Maximum number of ZIP entries, including the manifest and database.</summary>
    public const int MaxArchiveEntryCount = 10_000;

    /// <summary>Maximum uncompressed size of any one ZIP entry.</summary>
    public const long MaxEntryUncompressedBytes = 256L * 1024 * 1024;

    /// <summary>Maximum combined uncompressed size of all ZIP entries.</summary>
    public const long MaxTotalUncompressedBytes = 1L * 1024 * 1024 * 1024;

    /// <summary>Maximum allowed uncompressed-to-compressed ratio for a non-empty entry.</summary>
    public const long MaxCompressionRatio = 1_000;

    private const string ResourceLimitError =
        "The workspace package exceeds the supported resource limits.";

    public ZipWorkspacePackageReader(Func<string, IWorkspaceRepository> repositoryFactory)
    {
        ArgumentNullException.ThrowIfNull(repositoryFactory);
        _repositoryFactory = repositoryFactory;
    }

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
            ValidatePackageSize(packageStream.Length);
            archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
            ValidateArchiveResources(archive);
            var readBudget = new ArchiveReadBudget(MaxTotalUncompressedBytes);

            var manifestEntry = archive.GetEntry("manifest.json");
            var databaseEntry = archive.GetEntry("workspace.db");
            if (manifestEntry is null || databaseEntry is null)
            {
                return WorkspacePackageReadResult.Failure("The selected file is not a readable FusionCanvas workspace package.");
            }

            if (manifestEntry.Length > MaxManifestBytes)
            {
                throw new WorkspacePackageResourceLimitException();
            }

            WorkspacePackageManifest? manifest;
            await using (var manifestStream = OpenBoundedEntryStream(manifestEntry, readBudget))
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
            ValidateManifestResources(manifest);
            temporaryDirectory = Directory.CreateTempSubdirectory("fusioncanvas-import-");
            var databasePath = Path.Combine(temporaryDirectory.FullName, "workspace.db");
            progress?.Report(new WorkspaceTransferProgress("Reading workspace data", 0, 1));
            await using (var databaseInput = OpenBoundedEntryStream(databaseEntry, readBudget))
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

            var snapshot = await _repositoryFactory(databasePath)
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
                var normalizedPath = ManagedWorkspacePath.Normalize(file.Path);
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
                    _ => Task.FromResult<Stream>(OpenBoundedEntryStream(entry, readBudget))));
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
        catch (WorkspacePackageResourceLimitException)
        {
            return WorkspacePackageReadResult.Failure(ResourceLimitError);
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException or SqliteException or ArgumentException or InvalidOperationException)
        {
            TechnicalDiagnostics.RecordFailure("Workspace package read", exception);
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

    private static void ValidatePackageSize(long packageBytes)
    {
        if (packageBytes < 0 || packageBytes > MaxPackageBytes)
        {
            throw new WorkspacePackageResourceLimitException();
        }
    }

    private static void ValidateArchiveResources(ZipArchive archive)
    {
        if (archive.Entries.Count > MaxArchiveEntryCount)
        {
            throw new WorkspacePackageResourceLimitException();
        }

        long totalUncompressedBytes = 0;
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (!paths.Add(path))
            {
                throw new InvalidDataException("The package contains duplicate entries.");
            }

            if (entry.Length < 0 || entry.CompressedLength < 0 || entry.Length > MaxEntryUncompressedBytes)
            {
                throw new WorkspacePackageResourceLimitException();
            }

            if (entry.Length > 0 &&
                (entry.CompressedLength == 0 ||
                 (entry.Length > entry.CompressedLength &&
                  entry.Length / entry.CompressedLength > MaxCompressionRatio)))
            {
                throw new WorkspacePackageResourceLimitException();
            }

            try
            {
                totalUncompressedBytes = checked(totalUncompressedBytes + entry.Length);
            }
            catch (OverflowException)
            {
                throw new WorkspacePackageResourceLimitException();
            }

            if (path is "manifest.json" or "workspace.db")
            {
                continue;
            }

            if (!path.StartsWith("files/", StringComparison.Ordinal) || path.EndsWith('/'))
            {
                throw new InvalidDataException("The package contains an invalid entry.");
            }

            ManagedWorkspacePath.Normalize(path["files/".Length..]);
        }

        if (totalUncompressedBytes > MaxTotalUncompressedBytes)
        {
            throw new WorkspacePackageResourceLimitException();
        }

    }

    private static void ValidateManifestPaths(WorkspacePackageManifest manifest)
    {
        foreach (var file in manifest.Files)
        {
            ManagedWorkspacePath.Normalize(file.Path);
        }

        foreach (var file in manifest.MissingFiles)
        {
            ManagedWorkspacePath.Normalize(file);
        }
    }

    private static void ValidateManifestResources(WorkspacePackageManifest manifest)
    {
        long totalFileBytes = 0;
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in manifest.Files)
        {
            var normalizedPath = ManagedWorkspacePath.Normalize(file.Path);
            if (file.Size < 0 || file.Size > MaxEntryUncompressedBytes || !paths.Add(normalizedPath))
            {
                throw new WorkspacePackageResourceLimitException();
            }

            try
            {
                totalFileBytes = checked(totalFileBytes + file.Size);
            }
            catch (OverflowException)
            {
                throw new WorkspacePackageResourceLimitException();
            }
        }

        if (totalFileBytes > MaxTotalUncompressedBytes)
        {
            throw new WorkspacePackageResourceLimitException();
        }
    }

    private static Stream OpenBoundedEntryStream(
        ZipArchiveEntry entry,
        ArchiveReadBudget readBudget) =>
        new BoundedArchiveEntryStream(entry.Open(), entry.Length, readBudget);

    private static void TryDeleteDirectory(DirectoryInfo directory)
    {
        try
        {
            if (directory.Exists)
            {
                directory.Delete(recursive: true);
            }
        }
        catch (IOException exception)
        {
            TechnicalDiagnostics.RecordFailure("Workspace package temporary-directory cleanup", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            TechnicalDiagnostics.RecordFailure("Workspace package temporary-directory cleanup", exception);
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

    private sealed class ArchiveReadBudget(long maximumBytes)
    {
        private long _remainingBytes = maximumBytes;

        public void Consume(long bytes)
        {
            if (bytes <= 0)
            {
                return;
            }

            while (true)
            {
                var remaining = Volatile.Read(ref _remainingBytes);
                if (bytes > remaining ||
                    Interlocked.CompareExchange(ref _remainingBytes, remaining - bytes, remaining) == remaining)
                {
                    if (bytes > remaining)
                    {
                        throw new WorkspacePackageResourceLimitException();
                    }

                    return;
                }
            }
        }
    }

    private sealed class BoundedArchiveEntryStream(
        Stream inner,
        long declaredLength,
        ArchiveReadBudget readBudget) : Stream
    {
        private long _readBytes;
        private bool _endChecked;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => declaredLength;
        public override long Position
        {
            get => _readBytes;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var allowed = GetAllowedCount(buffer.Length);
            if (allowed == 0)
            {
                EnsureEndOfEntry();
                return 0;
            }

            var read = inner.Read(buffer[..allowed]);
            RecordRead(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var allowed = GetAllowedCount(buffer.Length);
            if (allowed == 0)
            {
                await EnsureEndOfEntryAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            }

            var read = await inner.ReadAsync(buffer[..allowed], cancellationToken).ConfigureAwait(false);
            RecordRead(read);
            return read;
        }

        public override int ReadByte()
        {
            Span<byte> buffer = stackalloc byte[1];
            return Read(buffer) == 0 ? -1 : buffer[0];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int GetAllowedCount(int requested)
        {
            if (requested == 0)
            {
                return 0;
            }

            var remaining = declaredLength - _readBytes;
            if (remaining <= 0)
            {
                return 0;
            }

            return (int)Math.Min(remaining, requested);
        }

        private void RecordRead(int read)
        {
            if (read <= 0)
            {
                return;
            }

            _readBytes += read;
            readBudget.Consume(read);
        }

        private void EnsureEndOfEntry()
        {
            if (_endChecked)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[1];
            if (inner.Read(buffer) > 0)
            {
                throw new WorkspacePackageResourceLimitException();
            }

            _endChecked = true;
        }

        private async ValueTask EnsureEndOfEntryAsync(CancellationToken cancellationToken)
        {
            if (_endChecked)
            {
                return;
            }

            var buffer = new byte[1];
            if (await inner.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false) > 0)
            {
                throw new WorkspacePackageResourceLimitException();
            }

            _endChecked = true;
        }
    }

    private sealed class WorkspacePackageResourceLimitException : Exception
    {
        public WorkspacePackageResourceLimitException()
            : base(ResourceLimitError)
        {
        }
    }
}
