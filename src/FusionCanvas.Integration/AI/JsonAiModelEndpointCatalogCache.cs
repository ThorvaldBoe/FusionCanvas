using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Integration.AI;

public sealed class JsonAiModelEndpointCatalogCache : IAiModelEndpointCatalogCache
{
    private const int SupportedVersion = 1;
    private const long MaximumCacheBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private readonly Func<DateTimeOffset> _clock;

    public JsonAiModelEndpointCatalogCache(string directoryPath, Func<DateTimeOffset>? clock = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("The endpoint cache directory must not be empty.", nameof(directoryPath));
        }

        DirectoryPath = Path.GetFullPath(directoryPath);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public string DirectoryPath { get; }

    public async Task<AiModelEndpointCatalog?> LoadAsync(
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        cancellationToken.ThrowIfCancellationRequested();
        var path = PathFor(modelId, requireZeroDataRetention);
        if (!File.Exists(path)) return null;

        try
        {
            var file = new FileInfo(path);
            if (file.Length is <= 0 or > MaximumCacheBytes) return null;
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var document = await JsonSerializer.DeserializeAsync<CacheDocument>(stream, Options, cancellationToken).ConfigureAwait(false);
            if (document is null || document.Version != SupportedVersion ||
                !string.Equals(document.Catalog.ModelId, modelId, StringComparison.Ordinal) ||
                document.Catalog.RequireZeroDataRetention != requireZeroDataRetention)
            {
                return null;
            }

            return document.Catalog with
            {
                IsStale = _clock() - document.Catalog.RetrievedAt > StaleAfter,
                Endpoints = document.Catalog.Endpoints.Select(endpoint => endpoint with
                {
                    IsStale = _clock() - document.Catalog.RetrievedAt > StaleAfter
                }).ToArray()
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            TechnicalDiagnostics.RecordFailure("AI model endpoint cache load", exception);
            return null;
        }
    }

    public async Task SaveAsync(AiModelEndpointCatalog catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(DirectoryPath);
        var path = PathFor(catalog.ModelId, catalog.RequireZeroDataRetention);
        var tempPath = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new CacheDocument(SupportedVersion, catalog with
                    {
                        IsStale = false,
                        Endpoints = catalog.Endpoints.Select(endpoint => endpoint with { IsStale = false }).ToArray()
                    }),
                    Options,
                    cancellationToken).ConfigureAwait(false);
            }

            if (new FileInfo(tempPath).Length > MaximumCacheBytes)
                throw new InvalidDataException("The endpoint catalog exceeds the cache size limit.");
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private string PathFor(string modelId, bool requireZeroDataRetention)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(modelId))).ToLowerInvariant();
        return Path.Combine(DirectoryPath, $"endpoints-{(requireZeroDataRetention ? "zdr" : "all")}-{hash}.json");
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception exception) { TechnicalDiagnostics.RecordFailure("AI endpoint cache temporary-file cleanup", exception); }
    }

    private sealed record CacheDocument(int Version, AiModelEndpointCatalog Catalog);
}
