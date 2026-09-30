using FusionCanvas.Domain.Assets;
using FusionCanvas.Integration.Files;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Integration.Tests.Files;

public class LocalWorkspaceFileStoreTests
{
    [Fact]
    public async Task RestoreAsync_CreatesFileAndSkipsExistingWithoutOverwriting()
    {
        using var root = new TemporaryDirectory();
        var workspaceRoot = root.GetPath("workspace");
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        using var createdContent = new MemoryStream([1, 2, 3]);
        using var skippedContent = new MemoryStream([9]);

        var created = await store.RestoreAsync(
            "assets/restored.png",
            createdContent,
            TestContext.Current.CancellationToken);
        var skipped = await store.RestoreAsync(
            "assets/restored.png",
            skippedContent,
            TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileRestoreOutcome.Created, created);
        Assert.Equal(WorkspaceFileRestoreOutcome.SkippedExisting, skipped);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(workspaceRoot, "assets", "restored.png"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestoreAsync_RejectsTraversalOutsideWorkspaceRoot()
    {
        using var root = new TemporaryDirectory();
        var store = new LocalWorkspaceFileStore(root.GetPath("workspace"));
        using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.RestoreAsync(
            "../escape.png",
            content,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ImportAsync_CopiesSourceFileIntoManagedWorkspace()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.png");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "image-bytes", TestContext.Current.CancellationToken);

        var store = new LocalWorkspaceFileStore(workspaceRoot);

        var imported = await store.ImportAsync(
            sourcePath,
            AssetKind.ExportedImage,
            TestContext.Current.CancellationToken);

        Assert.Equal("source.png", imported.Name);
        Assert.Equal(Path.GetFullPath(sourcePath), imported.OriginalSourcePath);
        Assert.True(File.Exists(imported.FullPath));
        Assert.True(store.Exists(imported.WorkspaceRelativePath));
        Assert.NotEqual(sourcePath, imported.FullPath);
        Assert.False(Path.IsPathRooted(imported.WorkspaceRelativePath));
        Assert.StartsWith($"assets/{DateTimeOffset.UtcNow:yyyy}", imported.WorkspaceRelativePath);
    }

    [Fact]
    public void Constructor_CreatesManagedWorkspaceRoot()
    {
        using var tempDirectory = new TemporaryDirectory();
        var workspaceRoot = tempDirectory.GetPath("workspace");

        var store = new LocalWorkspaceFileStore(workspaceRoot);

        Assert.Equal(Path.GetFullPath(workspaceRoot), store.WorkspaceRoot);
        Assert.True(Directory.Exists(workspaceRoot));
    }

    [Fact]
    public void Exists_ReturnsFalseForMissingManagedFile()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new LocalWorkspaceFileStore(tempDirectory.GetPath("workspace"));

        Assert.False(store.Exists(Path.Combine("assets", "missing.png")));
    }

    [Fact]
    public void Exists_ReturnsFalseForPathOutsideWorkspace()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new LocalWorkspaceFileStore(tempDirectory.GetPath("workspace"));

        Assert.False(store.Exists(Path.Combine("..", "workspace-other", "asset.png")));
        Assert.False(store.Exists(""));
        Assert.False(store.Exists(Path.GetFullPath(tempDirectory.GetPath("outside.png"))));
    }

    [Fact]
    public async Task ImportAsync_PreservesOriginalSourceOnlyAsTraceabilityMetadata()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.svg");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "<svg />", TestContext.Current.CancellationToken);
        var store = new LocalWorkspaceFileStore(workspaceRoot);

        var imported = await store.ImportAsync(
            sourcePath,
            AssetKind.Svg,
            TestContext.Current.CancellationToken);
        File.Delete(sourcePath);

        Assert.Equal(Path.GetFullPath(sourcePath), imported.OriginalSourcePath);
        Assert.True(store.Exists(imported.WorkspaceRelativePath));
        Assert.True(File.Exists(imported.FullPath));
    }

    [Fact]
    public async Task SaveAsync_RemovesPartialFileWhenContentCopyFails()
    {
        using var tempDirectory = new TemporaryDirectory();
        var workspaceRoot = tempDirectory.GetPath("workspace");
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        using var content = new FailingReadStream([1, 2, 3]);

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(
            "generated.png",
            AssetKind.ExportedImage,
            content,
            TestContext.Current.CancellationToken));

        Assert.Empty(Directory.EnumerateFiles(
            Path.Combine(workspaceRoot, "assets"),
            "*",
            SearchOption.AllDirectories));
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsReadableStreamWithinWorkspaceBoundary()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.png");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "preview-bytes", TestContext.Current.CancellationToken);
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        var imported = await store.ImportAsync(sourcePath, AssetKind.ExportedImage, TestContext.Current.CancellationToken);

        await using var stream = await store.OpenReadAsync(imported.WorkspaceRelativePath, TestContext.Current.CancellationToken);

        using var reader = new StreamReader(stream);
        Assert.Equal("preview-bytes", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenReadAsync_ReleasesFileHandleWhenStreamDisposed()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.png");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "preview-bytes", TestContext.Current.CancellationToken);
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        var imported = await store.ImportAsync(sourcePath, AssetKind.ExportedImage, TestContext.Current.CancellationToken);

        {
            await using var stream = await store.OpenReadAsync(imported.WorkspaceRelativePath, TestContext.Current.CancellationToken);
        }

        Assert.True(store.TryDelete(imported.WorkspaceRelativePath));
    }

    [Fact]
    public async Task OpenReadAsync_RejectsTraversalAttempt()
    {
        using var tempDirectory = new TemporaryDirectory();
        var workspaceRoot = tempDirectory.GetPath("workspace");
        var store = new LocalWorkspaceFileStore(workspaceRoot);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.OpenReadAsync("../escape.png", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenReadAsync_ReportsMissingSource()
    {
        using var tempDirectory = new TemporaryDirectory();
        var workspaceRoot = tempDirectory.GetPath("workspace");
        var store = new LocalWorkspaceFileStore(workspaceRoot);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => store.OpenReadAsync("assets/missing.png", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportCopyAsync_CopiesIdenticalBytesAndLeavesSourceUnchanged()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.png");
        var destinationPath = tempDirectory.GetPath("export.png");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "export-bytes", TestContext.Current.CancellationToken);
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        var imported = await store.ImportAsync(sourcePath, AssetKind.ExportedImage, TestContext.Current.CancellationToken);

        await store.ExportCopyAsync(imported.WorkspaceRelativePath, destinationPath, TestContext.Current.CancellationToken);

        Assert.Equal("export-bytes", await File.ReadAllTextAsync(destinationPath, TestContext.Current.CancellationToken));
        Assert.True(store.Exists(imported.WorkspaceRelativePath));
    }

    [Fact]
    public async Task ExportCopyAsync_RejectsSameSourceAndDestination()
    {
        using var tempDirectory = new TemporaryDirectory();
        var sourcePath = tempDirectory.GetPath("source.png");
        var workspaceRoot = tempDirectory.GetPath("workspace");
        await File.WriteAllTextAsync(sourcePath, "export-bytes", TestContext.Current.CancellationToken);
        var store = new LocalWorkspaceFileStore(workspaceRoot);
        var imported = await store.ImportAsync(sourcePath, AssetKind.ExportedImage, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ExportCopyAsync(imported.WorkspaceRelativePath, imported.FullPath, TestContext.Current.CancellationToken));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string path) => Path.Combine(_directory.FullName, path);

        public void Dispose() => _directory.Delete(recursive: true);
    }

    private sealed class FailingReadStream(byte[] initialBytes) : Stream
    {
        private bool _hasReturnedInitialBytes;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => initialBytes.Length;
        public override long Position { get; set; }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_hasReturnedInitialBytes)
            {
                throw new IOException("Simulated content read failure.");
            }

            _hasReturnedInitialBytes = true;
            var bytesToCopy = Math.Min(count, initialBytes.Length);
            initialBytes.AsSpan().CopyTo(buffer.AsSpan(offset, bytesToCopy));
            Position += bytesToCopy;
            return bytesToCopy;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_hasReturnedInitialBytes)
            {
                return ValueTask.FromException<int>(new IOException("Simulated content read failure."));
            }

            _hasReturnedInitialBytes = true;
            var bytesToCopy = Math.Min(buffer.Length, initialBytes.Length);
            initialBytes.AsMemory(0, bytesToCopy).CopyTo(buffer);
            Position += bytesToCopy;
            return ValueTask.FromResult(bytesToCopy);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
