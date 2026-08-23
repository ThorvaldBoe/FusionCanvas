namespace FusionCanvas.Application.Catalog;

public sealed record OfferingSetupCounts(int ActiveVariants, int ActiveDesignAreas, int ActiveMockupTemplates)
{
    public bool VariantsComplete => ActiveVariants > 0;
    public bool DesignAreasComplete => ActiveDesignAreas > 0;
    public bool MockupTemplatesComplete => ActiveMockupTemplates > 0;
}
