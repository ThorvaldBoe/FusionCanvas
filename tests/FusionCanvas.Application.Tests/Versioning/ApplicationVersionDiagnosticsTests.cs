using FusionCanvas.Application.Versioning;

namespace FusionCanvas.Application.Tests.Versioning;

public class ApplicationVersionDiagnosticsTests
{
    [Fact]
    public void TryParse_SplitsProductVersionAndCommitOnPlus()
    {
        var parsed = ApplicationVersionDiagnostics.TryParse("0.1.42+g3f91c2a", out var info);

        Assert.True(parsed);
        Assert.Equal("0.1.42", info.ProductVersion);
        Assert.Equal("0.1.42+g3f91c2a", info.InformationalVersion);
        Assert.Equal("3f91c2a", info.CommitId);
        Assert.True(info.IsCommitKnown);
    }

    [Fact]
    public void TryParse_StripsOnlyTheGitPrefixG()
    {
        ApplicationVersionDiagnostics.TryParse("0.4.127+f78780f841", out var info);

        Assert.Equal("0.4.127", info.ProductVersion);
        Assert.Equal("f78780f841", info.CommitId);
    }

    [Fact]
    public void TryParse_HandlesMissingCommitMetadata()
    {
        ApplicationVersionDiagnostics.TryParse("0.1.42", out var info);

        Assert.Equal("0.1.42", info.ProductVersion);
        Assert.Equal("0.1.42", info.InformationalVersion);
        Assert.Equal(ApplicationVersionInfo.UnknownCommitId, info.CommitId);
        Assert.False(info.IsCommitKnown);
    }

    [Fact]
    public void TryParse_HandlesEmptyMetadataAfterPlus()
    {
        ApplicationVersionDiagnostics.TryParse("0.1.42+", out var info);

        Assert.Equal("0.1.42", info.ProductVersion);
        Assert.Equal(ApplicationVersionInfo.UnknownCommitId, info.CommitId);
    }

    [Fact]
    public void TryParse_RejectsNullOrWhitespaceAndReturnsUnknown()
    {
        Assert.False(ApplicationVersionDiagnostics.TryParse(null, out var info));
        Assert.Equal(ApplicationVersionInfo.Unknown, info);
        Assert.False(info.IsCommitKnown);

        Assert.False(ApplicationVersionDiagnostics.TryParse("   ", out info));
        Assert.Equal(ApplicationVersionInfo.Unknown, info);
    }

    [Fact]
    public void Format_ProducesTheExpectedDiagnosticBlock()
    {
        var info = new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a");

        var formatted = ApplicationVersionDiagnostics.Format(info, "Windows x64");

        Assert.Equal($"Version: 0.1.42{Environment.NewLine}Commit: 3f91c2a{Environment.NewLine}Platform: Windows x64", formatted);
    }

    [Fact]
    public void Format_ReportsUnknownCommitWhenMissing()
    {
        ApplicationVersionDiagnostics.TryParse("0.1.42", out var info);

        var formatted = ApplicationVersionDiagnostics.Format(info, "Linux X64");

        Assert.Contains("Version: 0.1.42", formatted);
        Assert.Contains("Commit: unknown", formatted);
        Assert.Contains("Platform: Linux X64", formatted);
    }

    [Fact]
    public void Format_ReportsUnknownCommitForUnknownInfo()
    {
        var formatted = ApplicationVersionDiagnostics.Format(ApplicationVersionInfo.Unknown, "Windows x64");

        Assert.Contains("Version: 0.0.0", formatted);
        Assert.Contains("Commit: unknown", formatted);
        Assert.Contains("Platform: Windows x64", formatted);
    }
}
