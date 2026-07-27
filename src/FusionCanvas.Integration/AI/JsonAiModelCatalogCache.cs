using System.Text.Json;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Integration.AI;

public sealed class JsonAiModelCatalogCache : IAiModelCatalogCache
{
    private const int SupportedVersion = 1;
    private const long MaximumCacheBytes = 8 * 1024 * 1024;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public JsonAiModelCatalogCache(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("The catalog cache directory must not be empty.", nameof(directoryPath));
        }

        DirectoryPath = Path.GetFullPath(directoryPath);
    }

    public string DirectoryPath { get; }

    public async Task<AiModelCatalog?> LoadAsync(
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = PathFor(requireZeroDataRetention);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var file = new FileInfo(path);
            if (file.Length is <= 0 or > MaximumCacheBytes)
            {
                return null;
            }

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var document = await JsonSerializer.DeserializeAsync<CacheDocument>(stream, Options, cancellationToken)
                .ConfigureAwait(false);
            if (document is null ||
                document.Version != SupportedVersion ||
                document.Catalog.RequireZeroDataRetention != requireZeroDataRetention)
            {
                return null;
            }

            return document.Catalog with
            {
                IsStale = DateTimeOffset.UtcNow - document.Catalog.RetrievedAt > StaleAfter
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DirectoryPath);

        var path = PathFor(catalog.RequireZeroDataRetention);
        var tempPath = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new CacheDocument(SupportedVersion, catalog with { IsStale = false }),
                    Options,
                    cancellationToken).ConfigureAwait(false);
            }

            if (new FileInfo(tempPath).Length > MaximumCacheBytes)
            {
                throw new InvalidDataException("The model catalog exceeds the cache size limit.");
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private string PathFor(bool requireZeroDataRetention) =>
        Path.Combine(DirectoryPath, requireZeroDataRetention ? "models-zdr.json" : "models-all.json");

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { }
    }

    private sealed record CacheDocument(int Version, AiModelCatalog Catalog);
}
