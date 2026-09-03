using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.App.SllGeneration;

internal sealed class NullSllDocumentCodec : ISllDocumentCodec
{
    public string Serialize(SllDocument document) =>
        throw new InvalidOperationException("SLL document serialization is not configured. The composition root must inject it.");

    public bool TryDeserialize(string json, out SllDocument? document)
    {
        document = null;
        return false;
    }
}
