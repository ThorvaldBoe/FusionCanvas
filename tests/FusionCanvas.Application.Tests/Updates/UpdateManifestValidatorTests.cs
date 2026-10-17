using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class UpdateManifestValidatorTests
{
    [Fact]
    public void IsSupported_AcceptsCanonicalStableAndPreviewAssets()
    {
        Assert.True(UpdateManifestValidator.IsSupported(Manifest("0.3.0"), UpdatePlatform.WindowsX64));
        Assert.True(UpdateManifestValidator.IsSupported(Manifest("0.4.0-preview.2"), UpdatePlatform.WindowsX64));
    }

    [Fact]
    public void IsSupported_RejectsMismatchedTrackTagAndUnsafeUris()
    {
        var manifest = Manifest("0.3.0");
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { SchemaVersion = 2 }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { Sha256 = "bad" }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { TagName = "v0.4.0" }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { Track = UpdateTrack.Preview }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { BundleUri = new Uri("https://example.test/bundle") }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { InstallerUri = new Uri(manifest.InstallerUri + "?x=1") }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { SourceCommit = "bad" }, UpdatePlatform.WindowsX64));
    }

    [Fact]
    public void MatchesRelease_BindsManifestToDiscoveredTagAndTrack()
    {
        var manifest = Manifest("0.3.0");
        var release = new UpdateRelease("v0.3.0", "0.3.0", UpdateTrack.Stable, manifest.ReleaseUri,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0.json"));
        Assert.True(UpdateManifestValidator.MatchesRelease(manifest, release));
        Assert.False(UpdateManifestValidator.MatchesRelease(manifest, release with { Track = UpdateTrack.Preview }));
    }

    private static UpdateManifest Manifest(string version)
    {
        var isPreview = version.Contains("-preview.", StringComparison.Ordinal);
        var tag = "v" + version;
        var installer = $"FusionCanvas-{version}-win-x64-Setup.exe";
        return new UpdateManifest(UpdateManifestValidator.CurrentSchemaVersion, version, UpdatePlatform.WindowsX64,
            isPreview ? UpdateTrack.Preview : UpdateTrack.Stable, tag, new string('C', 40),
            new Uri($"https://github.com/ThorvaldBoe/FusionCanvas/releases/download/{tag}/{installer}"), new string('A', 64),
            new Uri($"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/{tag}"),
            new Uri($"https://github.com/ThorvaldBoe/FusionCanvas/releases/download/{tag}/{installer}.sigstore.json"));
    }
}
