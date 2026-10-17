using System.Text.Json;
using FusionCanvas.Application.Updates;
using Sigstore;

namespace FusionCanvas.Integration.Updates;

public sealed class GitHubAttestationVerifier : IInstallerAuthenticityVerifier
{
    public async Task VerifyAsync(string installerPath, string bundlePath, UpdateManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(bundlePath);
        ArgumentNullException.ThrowIfNull(manifest);
        if (!File.Exists(installerPath)) throw new FileNotFoundException("The downloaded update installer could not be found.", installerPath);
        if (!File.Exists(bundlePath)) throw new FileNotFoundException("The installer provenance bundle could not be found.", bundlePath);

        try
        {
            var verifier = new SigstoreVerifier();
            var result = await verifier.VerifyFileAsync(new FileInfo(installerPath), new FileInfo(bundlePath),
                new VerificationPolicy { CertificateIdentity = CreateCertificateIdentity(manifest) }, cancellationToken).ConfigureAwait(false);
            ValidateStatement(result.Statement, Path.GetFileName(installerPath), manifest);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException("The installer's GitHub provenance could not be verified. Setup was not launched.", exception);
        }
    }

    internal static CertificateIdentity CreateCertificateIdentity(UpdateManifest manifest)
    {
        var tagRef = $"refs/tags/{manifest.TagName}";
        var identity = CertificateIdentity.ForGitHubActions("ThorvaldBoe", "FusionCanvas", workflowRef: tagRef);
        identity.Extensions = new CertificateExtensionPolicy
        {
            SourceRepositoryUri = "https://github.com/ThorvaldBoe/FusionCanvas",
            SourceRepositoryRef = tagRef,
            SourceRepositoryDigest = manifest.SourceCommit,
            BuildConfigUri = $"https://github.com/ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml@{tagRef}"
        };
        return identity;
    }

    internal static void ValidateStatement(InTotoStatement? statement, string installerName, UpdateManifest manifest)
    {
        if (statement is null || statement.Subject is null || !statement.Subject.Any(subject =>
                string.Equals(subject.Name, installerName, StringComparison.Ordinal)
                && subject.Digest is not null && subject.Digest.TryGetValue("sha256", out var digest)
                && string.Equals(digest, manifest.Sha256, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The provenance bundle does not attest the downloaded installer digest. Setup was not launched.");

        if (statement.Predicate is not { } predicate)
            throw new InvalidOperationException("The provenance bundle does not contain source details. Setup was not launched.");
        if (!string.Equals(statement.PredicateType, "https://slsa.dev/provenance/v1", StringComparison.Ordinal)
            || !TryGetWorkflow(predicate, out var workflow)
            || GetString(workflow, "repository") != "https://github.com/ThorvaldBoe/FusionCanvas"
            || GetString(workflow, "path") != ".github/workflows/release-windows.yml"
            || GetString(workflow, "ref") != $"refs/tags/{manifest.TagName}")
            throw new InvalidOperationException("The provenance bundle does not match the release repository, tag, and source commit. Setup was not launched.");
    }

    private static bool TryGetWorkflow(JsonElement predicate, out JsonElement workflow)
    {
        workflow = default;
        return predicate.ValueKind == JsonValueKind.Object
            && predicate.TryGetProperty("buildDefinition", out var buildDefinition)
            && buildDefinition.TryGetProperty("externalParameters", out var externalParameters)
            && externalParameters.TryGetProperty("workflow", out workflow)
            && workflow.ValueKind == JsonValueKind.Object;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String) return property.GetString();
        return null;
    }
}
