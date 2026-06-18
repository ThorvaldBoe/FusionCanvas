using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.Integration.Tests;

public class LocalWorkspaceFileStoreTests
{
    [Fact]
    public async Task ImportAsync_CopiesSourceFileIntoManagedWorkspace()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var sourcePath = Path.Combine(tempDirectory.FullName, "source.png");
        var workspaceRoot = Path.Combine(tempDirectory.FullName, "workspace");
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
        var tempDirectory = Directory.CreateTempSubdirectory();
        var workspaceRoot = Path.Combine(tempDirectory.FullName, "workspace");

        var store = new LocalWorkspaceFileStore(workspaceRoot);

        Assert.Equal(Path.GetFullPath(workspaceRoot), store.WorkspaceRoot);
        Assert.True(Directory.Exists(workspaceRoot));
    }

    [Fact]
    public void Exists_ReturnsFalseForMissingManagedFile()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var store = new LocalWorkspaceFileStore(Path.Combine(tempDirectory.FullName, "workspace"));

        Assert.False(store.Exists(Path.Combine("assets", "missing.png")));
    }

    [Fact]
    public void Exists_ReturnsFalseForPathOutsideWorkspace()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var store = new LocalWorkspaceFileStore(Path.Combine(tempDirectory.FullName, "workspace"));

        Assert.False(store.Exists(Path.Combine("..", "workspace-other", "asset.png")));
        Assert.False(store.Exists(""));
        Assert.False(store.Exists(Path.GetFullPath(Path.Combine(tempDirectory.FullName, "outside.png"))));
    }

    [Fact]
    public async Task ImportAsync_PreservesOriginalSourceOnlyAsTraceabilityMetadata()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var sourcePath = Path.Combine(tempDirectory.FullName, "source.svg");
        var workspaceRoot = Path.Combine(tempDirectory.FullName, "workspace");
        await File.WriteAllTextAsync(sourcePath, "<svg />");
        var store = new LocalWorkspaceFileStore(workspaceRoot);

        var imported = await store.ImportAsync(sourcePath, AssetKind.Svg);
        File.Delete(sourcePath);

        Assert.Equal(Path.GetFullPath(sourcePath), imported.OriginalSourcePath);
        Assert.True(store.Exists(imported.WorkspaceRelativePath));
        Assert.True(File.Exists(imported.FullPath));
    }
}
