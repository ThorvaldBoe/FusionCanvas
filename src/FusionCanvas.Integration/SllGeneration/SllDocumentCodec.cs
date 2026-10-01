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
        if (string.IsNullOrWhiteSpace(json))
        {
            document = null;
            return false;
        }

        try
        {
            document = JsonSerializer.Deserialize<SllDocument>(json);
            if (document?.IsStructurallyComplete != true)
            {
                document = null;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }
}
