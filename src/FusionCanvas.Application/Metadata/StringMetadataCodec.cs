using System.Text.Json;

namespace FusionCanvas.Application.Metadata;

/// <summary>
/// Encodes the string-valued metadata used by context records while preserving unknown keys.
/// </summary>
public static class StringMetadataCodec
{
    public static Dictionary<string, string> Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new(StringComparer.Ordinal);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    public static string Serialize(IReadOnlyDictionary<string, string> metadata) =>
        metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata);

    public static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
