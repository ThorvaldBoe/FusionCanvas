using System.Text.Json;

namespace FusionCanvas.Application.Workspace;

internal static class ListingMetadataCodec
{
    public const string NotesKey = "notes";
    public const string IdeaKey = "idea";
    public const string PhraseKey = "phrase";
    public const string GraphicDirectionKey = "graphicDirection";
    public const string InheritedFromPrefix = "inheritedFrom:";

    public static string NormalizeName(string? value) => value?.Trim() ?? string.Empty;

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? ValidateName(string name) => string.IsNullOrWhiteSpace(name)
        ? "Listing title is required."
        : name.Contains('\n') || name.Contains('\r') ? "Listing title must be a single line." : null;

    public static string NormalizeSingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Trim();
    }

    public static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal)
            : new(StringComparer.Ordinal);
    }

    public static string SerializeMetadata(IReadOnlyDictionary<string, string> metadata) =>
        metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata);

    public static string? TryGetNotes(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(NotesKey, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            metadata.Remove(key);
        }
        else
        {
            metadata[key] = normalized;
        }
    }

    public static void ApplyContextMetadata(Dictionary<string, string> metadata, ListingContext context, bool replaceExplicitMetadata)
    {
        SetOptional(metadata, NotesKey, context.Notes);
        if (context.Metadata is null)
        {
            return;
        }

        foreach (var pair in context.Metadata)
        {
            var key = pair.Key?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = NormalizeOptional(pair.Value);
            if (value is null)
            {
                metadata.Remove(key);
            }
            else
            {
                metadata[key] = value;
            }

            if (replaceExplicitMetadata)
            {
                metadata.Remove($"{InheritedFromPrefix}{key}");
            }
        }
    }
}
