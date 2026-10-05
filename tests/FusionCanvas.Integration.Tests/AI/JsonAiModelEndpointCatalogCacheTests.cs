using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public sealed class JsonAiModelEndpointCatalogCacheTests
{
    [Fact]
    public async Task RoundTripPreservesEndpointIdentityAndMarksOldDataStale()
    {
        using var temp = new TemporaryDirectory();
        var now = new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var cache = new JsonAiModelEndpointCatalogCache(temp.Path, () => now);
        var catalog = new AiModelEndpointCatalog(
            "vendor/model",
            true,
            now.AddHours(-25),
            [new AiModelEndpointDescriptor("vendor/model", "provider", "Provider", "provider/variant", null, 4096, 1024, ["temperature"], true, 0.1m, 0.2m, 12, 34)]);

        await cache.SaveAsync(catalog, TestContext.Current.CancellationToken);
        var loaded = await cache.LoadAsync("vendor/model", true, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.True(loaded!.IsStale);
        Assert.True(Assert.Single(loaded.Endpoints).IsStale);
        Assert.Equal("provider/variant", loaded.Endpoints[0].EndpointId);
        Assert.Null(await cache.LoadAsync("other/model", true, TestContext.Current.CancellationToken));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FusionCanvas-endpoints-" + Guid.NewGuid().ToString("N"));
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
