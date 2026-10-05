using System.Net.Http.Headers;
using System.Text.Json;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Integration.Updates;

public sealed class GitHubUpdateSource : IUpdateSource
{
    private const int MaximumManifestBytes = 64 * 1024;
    private static readonly Uri LatestManifestUri = new(
        "https://github.com/ThorvaldBoe/FusionCanvas/releases/latest/download/latest.json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public GitHubUpdateSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UpdateManifest?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestManifestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaximumManifestBytes)
        {
            throw new InvalidOperationException("The update manifest is larger than the supported response limit.");
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var manifest = new MemoryStream();
        var buffer = new byte[8 * 1024];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (manifest.Length + read > MaximumManifestBytes)
            {
                throw new InvalidOperationException("The update manifest is larger than the supported response limit.");
            }

            await manifest.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return JsonSerializer.Deserialize<UpdateManifest>(manifest.ToArray(), JsonOptions);
    }
}
