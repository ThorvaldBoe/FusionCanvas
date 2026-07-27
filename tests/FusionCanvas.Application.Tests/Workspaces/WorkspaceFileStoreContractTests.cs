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

    [Fact]
    public async Task RestoreAsync_CreatesOnceAndSkipsExistingContent()
    {
        IWorkspaceFileStore fileStore = new InMemoryWorkspaceFileStore();

        var created = await fileStore.RestoreAsync(
            "assets/restored.png",
            new MemoryStream([1, 2, 3]),
            TestContext.Current.CancellationToken);
        var skipped = await fileStore.RestoreAsync(
            "assets/restored.png",
            new MemoryStream([9, 9, 9]),
            TestContext.Current.CancellationToken);
        await using var restored = await fileStore.OpenReadAsync("assets/restored.png", TestContext.Current.CancellationToken);
        using var buffer = new MemoryStream();
        await restored.CopyToAsync(buffer, TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceFileRestoreOutcome.Created, created);
        Assert.Equal(WorkspaceFileRestoreOutcome.SkippedExisting, skipped);
        Assert.Equal([1, 2, 3], buffer.ToArray());
    }

    [Fact]
    public async Task RestoreAsync_RejectsTraversal()
    {
        IWorkspaceFileStore fileStore = new InMemoryWorkspaceFileStore();

        await Assert.ThrowsAsync<ArgumentException>(() => fileStore.RestoreAsync(
            "../escape.png",
            new MemoryStream([1]),
            TestContext.Current.CancellationToken));
    }

    private sealed class InMemoryWorkspaceFileStore : IWorkspaceFileStore
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public string WorkspaceRoot => @"C:\workspace";

        public Task<ManagedWorkspaceFile> ImportAsync(
            string sourcePath,
            AssetKind kind,
            CancellationToken cancellationToken = default)
        {
            var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
            _files[relativePath] = [];

            return Task.FromResult(new ManagedWorkspaceFile(
                Path.GetFileName(sourcePath),
                kind,
                relativePath,
                Path.Combine(WorkspaceRoot, "assets", Path.GetFileName(sourcePath)),
                sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => _files.ContainsKey(WorkspaceFileReference.Normalize(workspaceRelativePath));

        public bool TryDelete(string workspaceRelativePath) => _files.Remove(WorkspaceFileReference.Normalize(workspaceRelativePath));

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
        {
            var normalized = WorkspaceFileReference.Normalize(workspaceRelativePath);
            return Task.FromResult<Stream>(new MemoryStream(_files[normalized], writable: false));
        }

        public async Task<WorkspaceFileRestoreOutcome> RestoreAsync(
            string workspaceRelativePath,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            var normalized = WorkspaceFileReference.Normalize(workspaceRelativePath);
            if (_files.ContainsKey(normalized))
            {
                return WorkspaceFileRestoreOutcome.SkippedExisting;
            }

            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            _files[normalized] = buffer.ToArray();
            return WorkspaceFileRestoreOutcome.Created;
        }

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
