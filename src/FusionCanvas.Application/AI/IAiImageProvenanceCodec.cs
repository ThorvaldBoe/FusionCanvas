namespace FusionCanvas.Application.AI;

public interface IAiImageProvenanceCodec
{
    string Serialize(AiImageProvenance provenance);

    bool TryDeserialize(string? json, out AiImageProvenance? provenance);
}
