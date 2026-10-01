using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Tests.DesignFiles;

internal sealed class TestAiImageProvenanceCodec : IAiImageProvenanceCodec
{
    public string Serialize(AiImageProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        return "test-provenance";
    }

    public bool TryDeserialize(string? json, out AiImageProvenance? provenance)
    {
        provenance = null;
        return false;
    }
}
