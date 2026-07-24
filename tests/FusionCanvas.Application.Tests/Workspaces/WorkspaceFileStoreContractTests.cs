using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Application.Tests;

public class WorkspaceFileStoreContractTests
{
    [Fact]
    public async Task WorkspaceFileStoreContract_ExposesAuthoritativeManagedReference()
    {
        IWorkspaceFileStore fileStore = new InMemoryWorkspaceFileStore();

        var imported = await fileStore.ImportAsync(
            @"C:\imports\source.png",
            AssetKind.ExportedImage,
            TestContext.Current.CancellationToken);

        Assert.Equal("source.png", imported.Name);
        Assert.Equal(AssetKind.ExportedImage, imported.Kind);
        Assert.Equal("assets/source.png", imported.WorkspaceRelativePath);
        Assert.Equal(Path.Combine(@"C:\workspace", "assets", "source.png"), imported.FullPath);
        Assert.Equal(@"C:\imports\source.png", imported.OriginalSourcePath);
        Assert.True(fileStore.Exists(imported.WorkspaceRelativePath));
        Assert.False(fileStore.Exists("assets/missing.png"));
    }

    private sealed class InMemoryWorkspaceFileStore : IWorkspaceFileStore
    {
        private readonly HashSet<string> _existingReferences = [];

        public string WorkspaceRoot => @"C:\workspace";

        public Task<ManagedWorkspaceFile> ImportAsync(
            string sourcePath,
            AssetKind kind,
            CancellationToken cancellationToken = default)
        {
            var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
            _existingReferences.Add(relativePath);

            return Task.FromResult(new ManagedWorkspaceFile(
                Path.GetFileName(sourcePath),
                kind,
                relativePath,
                Path.Combine(WorkspaceRoot, "assets", Path.GetFileName(sourcePath)),
                sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => _existingReferences.Contains(workspaceRelativePath);

        public bool TryDelete(string workspaceRelativePath) => _existingReferences.Remove(workspaceRelativePath);

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
