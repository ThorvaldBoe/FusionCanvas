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
