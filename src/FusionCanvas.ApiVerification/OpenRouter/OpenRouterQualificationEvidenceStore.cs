using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FusionCanvas.ApiVerification.Sessions;

namespace FusionCanvas.ApiVerification.OpenRouter;

internal sealed record OpenRouterQualificationEvidence(
    string ModelId,
    string Operation,
    string ProfileFingerprint,
    string Status,
    decimal? ActualCostUsd,
    DateTimeOffset VerifiedAt);

internal sealed class OpenRouterQualificationEvidenceStore(string dataDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private string EvidencePath => Path.Combine(dataDirectory, "openrouter-qualification.json");

    public static string CreateFingerprint<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));

    public static string GetAdapterRevision(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.ManifestModule.ModuleVersionId.ToString("N");
    }

    public async Task SaveAsync(OpenRouterQualificationEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Directory.CreateDirectory(dataDirectory);
        var entries = await ReadAsync(cancellationToken).ConfigureAwait(false);
        entries.RemoveAll(entry => entry.ModelId.Equals(evidence.ModelId, StringComparison.OrdinalIgnoreCase)
            && entry.Operation.Equals(evidence.Operation, StringComparison.Ordinal)
            && entry.ProfileFingerprint.Equals(evidence.ProfileFingerprint, StringComparison.Ordinal));
        entries.Add(evidence);

        await AtomicTextFile.WriteAsync(EvidencePath, JsonSerializer.Serialize(entries, JsonOptions), cancellationToken).ConfigureAwait(false);
    }

    public async Task<OpenRouterQualificationEvidence?> FindAsync(
        string modelId,
        string operation,
        string profileFingerprint,
        CancellationToken cancellationToken = default)
    {
        var entries = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return entries.LastOrDefault(entry => entry.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase)
            && entry.Operation.Equals(operation, StringComparison.Ordinal)
            && entry.ProfileFingerprint.Equals(profileFingerprint, StringComparison.Ordinal));
    }

    private async Task<List<OpenRouterQualificationEvidence>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(EvidencePath))
            return [];

        await using var stream = File.OpenRead(EvidencePath);
        return await JsonSerializer.DeserializeAsync<List<OpenRouterQualificationEvidence>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }
}
