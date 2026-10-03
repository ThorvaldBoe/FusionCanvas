namespace FusionCanvas.Application.Catalog;

public sealed record OfferingReadinessSummary(
    int ActiveVariantCount,
    int ActiveDesignAreaCount,
    int ActiveMockupTemplateCount,
    int ReadyMockupTemplateCount,
    OfferingReadinessStatus Status,
    IReadOnlyList<OfferingReadinessIssue> Issues)
{
    public bool IsReadyForMockupGeneration => Status == OfferingReadinessStatus.ReadyForMockupGeneration;
}
