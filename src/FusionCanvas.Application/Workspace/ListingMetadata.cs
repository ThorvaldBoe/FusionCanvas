using System.Text.Json;

namespace FusionCanvas.Application.Workspace;

public static class ListingMetadata
{
    public const string NotesKey = "notes";

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
}
