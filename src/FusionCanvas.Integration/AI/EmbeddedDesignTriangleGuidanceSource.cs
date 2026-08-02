using FusionCanvas.Application.ConceptRefinement;

namespace FusionCanvas.Integration.AI;

public sealed class EmbeddedDesignTriangleGuidanceSource : IDesignTriangleGuidanceSource
{
    internal const string ResourceName =
        "FusionCanvas.Integration.AI.DesignTriangleGuidance.md";

    public string Load()
    {
        using var stream = typeof(EmbeddedDesignTriangleGuidanceSource).Assembly
            .GetManifestResourceStream(ResourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}