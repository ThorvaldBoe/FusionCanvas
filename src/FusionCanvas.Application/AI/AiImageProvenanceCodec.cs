using System.Text.Json;

namespace FusionCanvas.Application.AI;

public static class AiImageProvenanceCodec
{
    public const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    public static string Serialize(AiImageProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        return JsonSerializer.Serialize(new Envelope(CurrentVersion, provenance), Options);
    }

    public static bool TryDeserialize(string? json, out AiImageProvenance? provenance)
    {
        provenance = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Options);
            if (envelope is null || envelope.Version != CurrentVersion || envelope.Provenance is null) return false;
            provenance = envelope.Provenance;
            return true;
        }
        catch (JsonException) { return false; }
        catch (NotSupportedException) { return false; }
    }

    private sealed record Envelope(int Version, AiImageProvenance? Provenance);
}
