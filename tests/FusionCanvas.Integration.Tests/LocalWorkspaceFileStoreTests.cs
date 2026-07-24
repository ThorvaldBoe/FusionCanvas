using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.Integration.Tests;

public class LocalWorkspaceFileStoreTests
{
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
}
