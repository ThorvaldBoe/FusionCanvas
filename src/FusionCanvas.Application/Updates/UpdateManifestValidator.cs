namespace FusionCanvas.Application.Updates;

public static class UpdateManifestValidator
{
    public const int CurrentSchemaVersion = 3;
    public const string Repository = "ThorvaldBoe/FusionCanvas";
    public const string ReleaseWorkflow = "ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml";
    public static Uri ReleasesApiUri { get; } = new("https://api.github.com/repos/ThorvaldBoe/FusionCanvas/releases?per_page=100");

    public static bool IsSupported(UpdateManifest? manifest, string platform) =>
        manifest is not null && manifest.SchemaVersion == CurrentSchemaVersion
        && string.Equals(manifest.Platform, platform, StringComparison.Ordinal)
        && Enum.IsDefined(manifest.Track)
        && UpdateProductVersion.TryParse(manifest.ProductVersion, out var version)
        && string.Equals(manifest.ProductVersion, version.ToString(), StringComparison.Ordinal)
        && string.Equals(manifest.TagName, "v" + version, StringComparison.Ordinal)
        && ((version.IsPreview && manifest.Track == UpdateTrack.Preview) || (!version.IsPreview && manifest.Track == UpdateTrack.Stable))
        && IsCommit(manifest.SourceCommit)
        && IsCanonicalAssetUri(manifest.InstallerUri, version, $"FusionCanvas-{version}-win-x64-Setup.exe")
        && IsCanonicalAssetUri(manifest.BundleUri, version, $"FusionCanvas-{version}-win-x64-Setup.exe.sigstore.json")
        && IsCanonicalReleaseUri(manifest.ReleaseUri, version)
        && IsSha256(manifest.Sha256);

    public static bool MatchesRelease(UpdateManifest manifest, UpdateRelease release) =>
        manifest.ProductVersion == release.ProductVersion && manifest.TagName == release.TagName
        && manifest.Track == release.Track
        && manifest.ReleaseUri == release.ReleaseUri;

    public static bool IsCanonicalAssetUri(Uri? uri, UpdateProductVersion version, string filename) =>
        IsGitHubUri(uri) && uri!.AbsolutePath == $"/{Repository}/releases/download/v{version}/{filename}";

    public static bool IsCanonicalReleaseUri(Uri? uri, UpdateProductVersion version) =>
        IsGitHubUri(uri) && uri!.AbsolutePath == $"/{Repository}/releases/tag/v{version}";

    private static bool IsGitHubUri(Uri? uri) => uri is { IsAbsoluteUri: true, Scheme: "https" }
        && uri.Authority.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    private static bool IsCommit(string? value) => value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool IsSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
}
