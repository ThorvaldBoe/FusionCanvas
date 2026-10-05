namespace FusionCanvas.Application.Updates;

public static class UpdateManifestValidator
{
    public static bool IsSupported(UpdateManifest? manifest, string platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        return manifest is not null
            && manifest.SchemaVersion == 1
            && string.Equals(manifest.Platform, platform, StringComparison.Ordinal)
            && StableProductVersion.TryParse(manifest.ProductVersion, out _)
            && IsGitHubHttpsUri(manifest.InstallerUri)
            && IsGitHubHttpsUri(manifest.ReleaseUri)
            && IsSha256(manifest.Sha256);
    }

    private static bool IsGitHubHttpsUri(Uri? uri) =>
        uri is not null
        && uri.IsAbsoluteUri
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase);

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
