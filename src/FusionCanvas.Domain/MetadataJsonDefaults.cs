namespace FusionCanvas.Domain;

public static class MetadataJsonDefaults
{
    public const string EmptyObject = "{}";

    public static string Normalize(string? metadataJson) =>
        string.IsNullOrWhiteSpace(metadataJson) ? EmptyObject : metadataJson;
}
