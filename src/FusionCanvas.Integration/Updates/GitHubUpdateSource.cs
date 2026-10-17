using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Integration.Updates;

public sealed class GitHubUpdateSource : IUpdateSource
{
    private const int MaximumManifestBytes = 64 * 1024;
    private const int PageSize = 100;
    private const int MaximumPages = 5;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient _httpClient;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public GitHubUpdateSource(HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<IReadOnlyList<UpdateRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        var releases = new List<UpdateRelease>();
        for (var page = 1; page <= MaximumPages; page++)
        {
            var uri = new Uri($"https://api.github.com/repos/{UpdateManifestValidator.Repository}/releases?per_page={PageSize}&page={page}");
            using var response = await SendJsonAsync(uri, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return releases;
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("GitHub returned an invalid release list.");
            var count = 0;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                count++;
                if (TryParseRelease(item, out var release)) releases.Add(release!);
            }
            if (count < PageSize) return releases;
            if (page == MaximumPages) throw new InvalidOperationException("GitHub release history exceeded the supported discovery limit.");
        }
        return releases;
    }

    public async Task<UpdateManifest?> GetManifestAsync(UpdateRelease release, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        if (!UpdateProductVersion.TryParse(release.ProductVersion, out var version)
            || !IsCanonicalManifestUri(release.ManifestUri, version)) throw new InvalidOperationException("The update manifest URL is not trusted.");
        using var response = await SendJsonAsync(release.ManifestUri, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaximumManifestBytes) throw new InvalidOperationException("The update manifest is larger than the supported response limit.");
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var bounded = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (bounded.Length + read > MaximumManifestBytes) throw new InvalidOperationException("The update manifest is larger than the supported response limit.");
            await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return JsonSerializer.Deserialize<UpdateManifest>(bounded.ToArray(), JsonOptions);
    }

    private async Task<HttpResponseMessage> SendJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("FusionCanvas-Updater/1.0");
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryParseRelease(JsonElement item, out UpdateRelease? release)
    {
        release = null;
        if (!TryString(item, "tag_name", out var tag) || !tag.StartsWith('v')
            || !UpdateProductVersion.TryParse(tag[1..], out var version) || tag != "v" + version
            || !TryBoolean(item, "prerelease", out var prerelease) || TryBoolean(item, "draft", out var draft) && draft
            || prerelease != version.IsPreview
            || !TryString(item, "html_url", out var html) || !Uri.TryCreate(html, UriKind.Absolute, out var releaseUri)
            || !UpdateManifestValidator.IsCanonicalReleaseUri(releaseUri, version)
            || !HasAsset(item, $"FusionCanvas-{version}.json")) return false;
        var track = prerelease ? UpdateTrack.Preview : UpdateTrack.Stable;
        var manifestUri = new Uri($"https://github.com/{UpdateManifestValidator.Repository}/releases/download/{tag}/FusionCanvas-{version}.json");
        release = new UpdateRelease(tag, version.ToString(), track, releaseUri!, manifestUri);
        return true;
    }

    private static bool HasAsset(JsonElement item, string name) => item.TryGetProperty("assets", out var assets)
        && assets.ValueKind == JsonValueKind.Array
        && assets.EnumerateArray().Any(asset => TryString(asset, "name", out var assetName) && assetName == name);

    private static bool IsCanonicalManifestUri(Uri uri, UpdateProductVersion version) =>
        uri.Scheme == "https" && uri.Authority.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath == $"/{UpdateManifestValidator.Repository}/releases/download/v{version}/FusionCanvas-{version}.json"
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    private static bool TryString(JsonElement item, string name, out string value)
    {
        value = string.Empty;
        return item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String && (value = property.GetString() ?? string.Empty).Length > 0;
    }

    private static bool TryBoolean(JsonElement item, string name, out bool value)
    {
        value = false;
        return item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var property)
            && (property.ValueKind is JsonValueKind.True or JsonValueKind.False) && (value = property.GetBoolean()) == property.GetBoolean();
    }
}
