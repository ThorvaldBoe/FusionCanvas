using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.Application.SllGeneration;

public interface ISllDocumentCodec
{
    string Serialize(SllDocument document);

    bool TryDeserialize(string json, out SllDocument? document);
}
