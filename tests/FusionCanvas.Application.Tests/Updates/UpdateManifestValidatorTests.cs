using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class UpdateManifestValidatorTests
{
    [Fact]
    public void IsSupported_RejectsNonGitHubAndNonHttpsSources()
    {
        var manifest = new UpdateManifest(
            UpdateManifestValidator.CurrentSchemaVersion,
            "0.3.0",
            UpdatePlatform.WindowsX64,
            new Uri("https://example.test/setup.exe"),
            new string('A', 64),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
            new string('B', 64));

        Assert.False(UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64));
    }

    [Fact]
    public void IsSupported_RequiresValidSchemaPlatformVersionAndChecksum()
    {
        var manifest = new UpdateManifest(
            UpdateManifestValidator.CurrentSchemaVersion,
            "0.3.0",
            UpdatePlatform.WindowsX64,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe"),
            new string('A', 64),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
            new string('B', 64));

        Assert.True(UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { SchemaVersion = 1 }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { Sha256 = "bad" }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { PublisherCertificateSha256 = "bad" }, UpdatePlatform.WindowsX64));
    }

    [Fact]
    public void IsSupported_RejectsLookalikeUrlsAndAcceptsOnlyCanonicalRedirects()
    {
        var manifest = new UpdateManifest(
            2,
            "0.3.0",
            UpdatePlatform.WindowsX64,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe?download=1"),
            new string('A', 64),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
            new string('B', 64));

        Assert.False(UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64));
        Assert.True(UpdateManifestValidator.IsCanonicalManifestRedirect(
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/latest.json")));
        Assert.False(UpdateManifestValidator.IsCanonicalManifestRedirect(
            new Uri("https://example.test/releases/download/v0.3.0/latest.json")));
        Assert.False(UpdateManifestValidator.IsCanonicalManifestRedirect(
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/latest.json?x=1")));
    }
}
