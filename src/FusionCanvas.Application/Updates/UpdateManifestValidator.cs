namespace FusionCanvas.Application.Updates;

public static class UpdateManifestValidator
{
    public const int CurrentSchemaVersion = 2;

    public static Uri DiscoveryUri { get; } = new(
        "https://github.com/ThorvaldBoe/FusionCanvas/releases/latest/download/latest.json");

    public static bool IsSupported(UpdateManifest? manifest, string platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        return manifest is not null
            && manifest.SchemaVersion == CurrentSchemaVersion
            && string.Equals(manifest.Platform, platform, StringComparison.Ordinal)
            && StableProductVersion.TryParse(manifest.ProductVersion, out var version)
            && string.Equals(manifest.ProductVersion, version.ToString(), StringComparison.Ordinal)
            && IsCanonicalInstallerUri(manifest.InstallerUri, version)
            && IsCanonicalReleaseUri(manifest.ReleaseUri, version)
            && IsSha256(manifest.Sha256)
            && IsSha256(manifest.PublisherCertificateSha256);
    }

    public static bool IsCanonicalManifestRedirect(Uri? uri)
    {
        if (!IsGitHubHttpsUri(uri))
        {
            return false;
        }

        var segments = GetPathSegments(uri!);
        return segments.Length == 6
            && string.Equals(segments[0], "ThorvaldBoe", StringComparison.Ordinal)
            && string.Equals(segments[1], "FusionCanvas", StringComparison.Ordinal)
            && string.Equals(segments[2], "releases", StringComparison.Ordinal)
            && string.Equals(segments[3], "download", StringComparison.Ordinal)
            && segments[4].StartsWith("v", StringComparison.Ordinal)
            && StableProductVersion.TryParse(segments[4][1..], out var version)
            && string.Equals(segments[4], "v" + version, StringComparison.Ordinal)
            && string.Equals(segments[5], "latest.json", StringComparison.Ordinal);
    }

    private static bool IsCanonicalInstallerUri(Uri? uri, StableProductVersion version)
    {
        var expectedPath = $"/ThorvaldBoe/FusionCanvas/releases/download/v{version}/FusionCanvas-{version}-win-x64-Setup.exe";
        return IsGitHubHttpsUri(uri)
            && string.Equals(uri!.AbsolutePath, expectedPath, StringComparison.Ordinal);
    }

    private static bool IsCanonicalReleaseUri(Uri? uri, StableProductVersion version)
    {
        var expectedPath = $"/ThorvaldBoe/FusionCanvas/releases/tag/v{version}";
        return IsGitHubHttpsUri(uri)
            && string.Equals(uri!.AbsolutePath, expectedPath, StringComparison.Ordinal);
    }

    private static bool IsGitHubHttpsUri(Uri? uri) =>
        uri is not null
        && uri.IsAbsoluteUri
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Authority, "github.com", StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);

    private static string[] GetPathSegments(Uri uri) =>
        uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static bool IsSha256(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length == 64
        && TryDecodeSha256(value);

    private static bool TryDecodeSha256(string value)
    {
        try
        {
            return Convert.FromHexString(value).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
