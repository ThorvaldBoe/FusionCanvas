using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using FusionCanvas.Application.Metadata;

namespace FusionCanvas.Application.Items;

internal static class ItemMetadataCodec
{
    public const string NotesKey = "notes";
    public const string IdeaKey = "idea";
    public const string ConceptIdeaKey = "concept.idea";
    public const string IdeaAudienceKey = "idea.audience";
    public const string PhraseKey = "phrase";
    public const string GraphicDirectionKey = "graphicDirection";
    public const string IdeaRatingKey = "idea.rating";
    public const string SllKey = "sll";
    public const string SllSourceFingerprintKey = "sll.sourceFingerprint";
    public const string ArtworkTargetIdKey = "design.artworkTargetId";
    public const string ArtworkTargetPreferenceKey = "design.artworkTargetPreference";
    public const string ArtworkTransparentBackgroundKey = "design.transparentBackground";
    public const string InheritedFromPrefix = "inheritedFrom:";

    public static string NormalizeName(string? value) => value?.Trim() ?? string.Empty;

    public static string? NormalizeOptional(string? value) => StringMetadataCodec.NormalizeOptional(value);

    public static string? ValidateName(string name) => name.Contains('\n') || name.Contains('\r')
        ? "Item title must be a single line."
        : null;

    public static string NormalizeSingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Trim();
    }

    public static Dictionary<string, string> ParseMetadata(string metadataJson) => StringMetadataCodec.Parse(metadataJson);

    public static IReadOnlyDictionary<string, string> SanitizeCreativeContextMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            return SanitizeCreativeContextMetadata(ParseMetadata(metadataJson));
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    public static IReadOnlyDictionary<string, string> SanitizeCreativeContextMetadata(
        IReadOnlyDictionary<string, string> metadata) =>
        metadata
            .Where(pair => !IsOperationalCreativeContextKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static bool IsOperationalCreativeContextKey(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        var compact = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized.StartsWith(InheritedFromPrefix.ToLowerInvariant(), StringComparison.Ordinal) ||
               compact is "id" or "createdat" or "updatedat" or "isarchived" or "status" ||
               compact.Contains("inherited", StringComparison.Ordinal) ||
               compact.Contains("path", StringComparison.Ordinal) ||
               compact.Contains("apikey", StringComparison.Ordinal) ||
               compact.Contains("credential", StringComparison.Ordinal) ||
               compact.Contains("password", StringComparison.Ordinal) ||
               compact.Contains("secret", StringComparison.Ordinal) ||
               compact.Contains("token", StringComparison.Ordinal);
    }

    public static string SerializeMetadata(IReadOnlyDictionary<string, string> metadata) => StringMetadataCodec.Serialize(metadata);

    public static string ComputeSllSourceFingerprint(string? idea, string? conceptIdea, string? phrase, string? graphicDirection)
    {
        var source = string.Join("\u001f", NormalizeOptional(idea) ?? "", NormalizeOptional(conceptIdea) ?? "", NormalizeOptional(phrase) ?? "", NormalizeOptional(graphicDirection) ?? "");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

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

    public static void SetOptional(Dictionary<string, string> metadata, string key, string? value) =>
        StringMetadataCodec.SetOptional(metadata, key, value);

    public static int GetIdeaRating(IReadOnlyDictionary<string, string> metadata) =>
        metadata.TryGetValue(IdeaRatingKey, out var value) && int.TryParse(value, out var rating) && rating is >= 1 and <= 5
            ? rating
            : 0;

    public static string? ValidateIdeaRating(int rating) =>
        rating is >= 0 and <= 5 ? null : "Idea rating must be between 0 and 5 stars.";

    public static void SetIdeaRating(Dictionary<string, string> metadata, int rating)
    {
        var error = ValidateIdeaRating(rating);
        if (error is not null) throw new ArgumentOutOfRangeException(nameof(rating), error);
        if (rating == 0) metadata.Remove(IdeaRatingKey);
        else metadata[IdeaRatingKey] = rating.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static void ApplyContextMetadata(Dictionary<string, string> metadata, ItemContext context, bool replaceExplicitMetadata)
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
