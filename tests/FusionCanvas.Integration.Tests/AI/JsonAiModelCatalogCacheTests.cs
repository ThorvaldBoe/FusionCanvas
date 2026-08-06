using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public class JsonAiModelCatalogCacheTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsByPrivacyPolicy()
    {
        using var directory = new TemporaryDirectory();
        var cache = new JsonAiModelCatalogCache(directory.Path);
        var catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"], [],
                10, 5, null, null, true, null)]);

        await cache.SaveAsync(catalog, TestContext.Current.CancellationToken);
        var loaded = await cache.LoadAsync(true, TestContext.Current.CancellationToken);
        var broader = await cache.LoadAsync(false, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Models);
        Assert.Null(broader);
    }

    [Fact]
    public async Task LoadAsync_CorruptOrOversizedCacheIsIgnored()
    {
        using var directory = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(directory.Path));
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "models-zdr.json"),
            "not json",
            TestContext.Current.CancellationToken);
        var cache = new JsonAiModelCatalogCache(directory.Path);

        var loaded = await cache.LoadAsync(true, TestContext.Current.CancellationToken);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadAsync_MarksOldCatalogsStaleAndRejectsUnsupportedVersions()
    {
        using var directory = new TemporaryDirectory();
        var cache = new JsonAiModelCatalogCache(directory.Path);
        var oldCatalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow.AddDays(-2),
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"], [],
                10, 5, null, null, true, null)]);

        await cache.SaveAsync(oldCatalog, TestContext.Current.CancellationToken);
        var loaded = await cache.LoadAsync(true, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsStale);

        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "models-zdr.json"),
            "{\"version\":99,\"catalog\":{}}",
            TestContext.Current.CancellationToken);

        Assert.Null(await cache.LoadAsync(true, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadAndSaveAsync_HonorCancellation()
    {
        using var directory = new TemporaryDirectory();
        var cache = new JsonAiModelCatalogCache(directory.Path);
        var cancelled = new CancellationToken(canceled: true);
        var catalog = new AiModelCatalog(true, DateTimeOffset.UtcNow, []);

        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.LoadAsync(true, cancelled));
        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.SaveAsync(catalog, cancelled));
    }

    [Fact]
    public async Task SaveAsync_FailsWhenCacheDirectoryIsNotWritable()
    {
        using var directory = new TemporaryDirectory();
        var occupiedPath = Path.Combine(directory.Path, "occupied");
        await File.WriteAllTextAsync(occupiedPath, "file", TestContext.Current.CancellationToken);
        var cache = new JsonAiModelCatalogCache(occupiedPath);

        await Assert.ThrowsAnyAsync<IOException>(() => cache.SaveAsync(
            new AiModelCatalog(true, DateTimeOffset.UtcNow, []),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadAsync_RejectsContentBeyondBound()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "models-zdr.json");
        await File.WriteAllTextAsync(
            path,
            new string('x', 8 * 1024 * 1024 + 1),
            TestContext.Current.CancellationToken);

        var loaded = await new JsonAiModelCatalogCache(directory.Path)
            .LoadAsync(true, TestContext.Current.CancellationToken);

        Assert.Null(loaded);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "FusionCanvas.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { }
        }
    }
}
