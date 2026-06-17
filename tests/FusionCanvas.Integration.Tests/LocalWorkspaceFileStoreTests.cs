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
        Assert.StartsWith(Path.Combine("assets", DateTimeOffset.UtcNow.ToString("yyyy")), imported.WorkspaceRelativePath);
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
    }
}
