using System.Text.Json;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.Integration.SllGeneration;

public sealed class SllDocumentCodec : ISllDocumentCodec
{
    public string Serialize(SllDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document);
    }

    public bool TryDeserialize(string json, out SllDocument? document)
    {
        try
        {
            document = JsonSerializer.Deserialize<SllDocument>(json);
            return document is not null;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }
}
