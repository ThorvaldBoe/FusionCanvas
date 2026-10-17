using System.Text.Json;
using System.Text.RegularExpressions;
using FusionCanvas.Application.Updates;
using FusionCanvas.Integration.Updates;
using Sigstore;

namespace FusionCanvas.Integration.Tests.Updates;

public sealed class GitHubAttestationVerifierTests
{
    private const string InstallerName = "FusionCanvas-1.2.3-win-x64-Setup.exe";
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void CertificateIdentityBindsRepositoryWorkflowTagAndSourceCommit()
    {
        var identity = GitHubAttestationVerifier.CreateCertificateIdentity(Manifest());

        Assert.NotNull(identity.SubjectAlternativeNamePattern);
        Assert.Matches(new Regex(identity.SubjectAlternativeNamePattern, RegexOptions.CultureInvariant),
            "https://github.com/ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml@refs/tags/v1.2.3");
        Assert.DoesNotMatch(new Regex(identity.SubjectAlternativeNamePattern, RegexOptions.CultureInvariant),
            "https://github.com/attacker/FusionCanvas/.github/workflows/release-windows.yml@refs/tags/v1.2.3");
        Assert.Equal("https://token.actions.githubusercontent.com", identity.Issuer);
        Assert.Equal("https://github.com/ThorvaldBoe/FusionCanvas", identity.Extensions!.SourceRepositoryUri);
        Assert.Equal("refs/tags/v1.2.3", identity.Extensions.SourceRepositoryRef);
        Assert.Equal(Manifest().SourceCommit, identity.Extensions.SourceRepositoryDigest);
        Assert.Equal("https://github.com/ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml@refs/tags/v1.2.3", identity.Extensions.BuildConfigUri);
    }

    [Fact]
    public void AcceptsMatchingGitHubProvenanceStatement()
    {
        GitHubAttestationVerifier.ValidateStatement(Statement(), InstallerName, Manifest());
    }

    [Theory]
    [InlineData("sha256", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "https://slsa.dev/provenance/v1", "https://github.com/ThorvaldBoe/FusionCanvas", ".github/workflows/release-windows.yml", "refs/tags/v1.2.3")]
    [InlineData("sha256", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "https://slsa.dev/provenance/v0.2", "https://github.com/ThorvaldBoe/FusionCanvas", ".github/workflows/release-windows.yml", "refs/tags/v1.2.3")]
    [InlineData("sha256", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "https://slsa.dev/provenance/v1", "https://github.com/attacker/FusionCanvas", ".github/workflows/release-windows.yml", "refs/tags/v1.2.3")]
    [InlineData("sha256", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "https://slsa.dev/provenance/v1", "https://github.com/ThorvaldBoe/FusionCanvas", ".github/workflows/other.yml", "refs/tags/v1.2.3")]
    [InlineData("sha256", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "https://slsa.dev/provenance/v1", "https://github.com/ThorvaldBoe/FusionCanvas", ".github/workflows/release-windows.yml", "refs/tags/v9.9.9")]
    public void RejectsMismatchedProvenanceStatement(
        string digestAlgorithm,
        string digest,
        string predicateType,
        string repository,
        string workflowPath,
        string sourceRef)
    {
        var statement = Statement(digestAlgorithm, digest, predicateType, repository, workflowPath, sourceRef);

        Assert.Throws<InvalidOperationException>(() => GitHubAttestationVerifier.ValidateStatement(statement, InstallerName, Manifest()));
    }

    [Fact]
    public void RejectsDifferentInstallerSubject()
    {
        var statement = JsonSerializer.Deserialize<InTotoStatement>(StatementJson(subjectName: "other-installer.exe"), JsonOptions);

        Assert.Throws<InvalidOperationException>(() => GitHubAttestationVerifier.ValidateStatement(statement, InstallerName, Manifest()));
    }

    [Fact]
    public async Task RejectsMissingOrMalformedBundleBeforeInstallerHandoff()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvasAttestationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var installer = Path.Combine(directory, InstallerName);
        var bundle = Path.Combine(directory, "bundle.sigstore.json");
        await File.WriteAllTextAsync(installer, "installer");
        var verifier = new GitHubAttestationVerifier();

        try
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() => verifier.VerifyAsync(installer, bundle, Manifest()));
            await File.WriteAllTextAsync(bundle, "not-json");
            await Assert.ThrowsAsync<InvalidOperationException>(() => verifier.VerifyAsync(installer, bundle, Manifest()));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static InTotoStatement? Statement(
        string algorithm = "sha256",
        string digest = Digest,
        string predicateType = "https://slsa.dev/provenance/v1",
        string repository = "https://github.com/ThorvaldBoe/FusionCanvas",
        string workflowPath = ".github/workflows/release-windows.yml",
        string sourceRef = "refs/tags/v1.2.3") =>
        JsonSerializer.Deserialize<InTotoStatement>(StatementJson(algorithm, digest, predicateType, repository, workflowPath, sourceRef), JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string StatementJson(
        string algorithm = "sha256",
        string digest = Digest,
        string predicateType = "https://slsa.dev/provenance/v1",
        string repository = "https://github.com/ThorvaldBoe/FusionCanvas",
        string workflowPath = ".github/workflows/release-windows.yml",
        string sourceRef = "refs/tags/v1.2.3",
        string subjectName = InstallerName) =>
        JsonSerializer.Serialize(new
        {
            _type = "https://in-toto.io/Statement/v1",
            subject = new[] { new { name = subjectName, digest = new Dictionary<string, string> { [algorithm] = digest } } },
            predicateType,
            predicate = new
            {
                buildDefinition = new
                {
                    externalParameters = new { workflow = new { repository, path = workflowPath, @ref = sourceRef } }
                }
            }
        });

    private static UpdateManifest Manifest() => new(
        SchemaVersion: 3,
        ProductVersion: "1.2.3",
        Platform: "win-x64",
        Track: UpdateTrack.Stable,
        TagName: "v1.2.3",
        SourceCommit: "0123456789abcdef0123456789abcdef01234567",
        InstallerUri: new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v1.2.3/FusionCanvas-1.2.3-win-x64-Setup.exe"),
        Sha256: Digest,
        ReleaseUri: new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v1.2.3"),
        BundleUri: new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v1.2.3/FusionCanvas-1.2.3-win-x64-Setup.exe.sigstore.json"));
}
