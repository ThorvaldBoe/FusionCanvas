using FusionCanvas.Domain.Workspace;
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
        await File.WriteAllTextAsync(sourcePath, "image-bytes");

        var store = new LocalWorkspaceFileStore(workspaceRoot);

        var imported = await store.ImportAsync(sourcePath, AssetKind.ExportedImage);

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
        await File.WriteAllTextAsync(sourcePath, "<svg />");
        var store = new LocalWorkspaceFileStore(workspaceRoot);

        var imported = await store.ImportAsync(sourcePath, AssetKind.Svg);
        File.Delete(sourcePath);

        Assert.Equal(Path.GetFullPath(sourcePath), imported.OriginalSourcePath);
        Assert.True(store.Exists(imported.WorkspaceRelativePath));
        Assert.True(File.Exists(imported.FullPath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string path) => Path.Combine(_directory.FullName, path);

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
