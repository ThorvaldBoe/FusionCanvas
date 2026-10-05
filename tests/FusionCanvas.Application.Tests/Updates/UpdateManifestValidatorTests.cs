using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class UpdateManifestValidatorTests
{
    [Fact]
    public void IsSupported_RejectsNonGitHubAndNonHttpsSources()
    {
        var manifest = new UpdateManifest(
            1,
            "0.3.0",
            UpdatePlatform.WindowsX64,
            new Uri("https://example.test/setup.exe"),
            new string('A', 64),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"));

        Assert.False(UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64));
    }

    [Fact]
    public void IsSupported_RequiresValidSchemaPlatformVersionAndChecksum()
    {
        var manifest = new UpdateManifest(
            1,
            "0.3.0",
            UpdatePlatform.WindowsX64,
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-Setup.exe"),
            new string('A', 64),
            new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"));

        Assert.True(UpdateManifestValidator.IsSupported(manifest, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { SchemaVersion = 2 }, UpdatePlatform.WindowsX64));
        Assert.False(UpdateManifestValidator.IsSupported(manifest with { Sha256 = "bad" }, UpdatePlatform.WindowsX64));
    }
}
