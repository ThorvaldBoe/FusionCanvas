using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.App;

namespace FusionCanvas.App.Stores;

public sealed record BlueprintOfferingCardViewModel(
    Guid Id,
    string Name,
    string FulfillmentContext,
    bool IsProviderNetwork,
    string Status,
    int VariantCount,
    int DesignAreaCount,
    int MockupTemplateCount)
{
    public string SetupSummary => $"{VariantCount} Variants · {DesignAreaCount} Design Areas · {MockupTemplateCount} Mockup Templates";
    public int ReadyMockupTemplateCount { get; init; }
    public string ReadinessSummary { get; init; } = "Catalog readiness is unavailable.";
    public IReadOnlyList<string> ReadinessGuidance { get; init; } = [];

    public static BlueprintOfferingCardViewModel From(BlueprintOfferingSetupSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var readiness = summary.Readiness;
        var status = summary.IsArchived
            ? "Archived"
            : readiness?.Status switch
            {
                OfferingReadinessStatus.ReadyForMockupGeneration => "Ready for mockup generation",
                OfferingReadinessStatus.NeedsAttention => "Needs attention",
                OfferingReadinessStatus.Incomplete => "Setup incomplete",
                _ => summary.Counts.VariantsComplete && summary.Counts.DesignAreasComplete && summary.Counts.MockupTemplatesComplete
                    ? "Ready"
                    : "Setup incomplete"
            };
        var card = new BlueprintOfferingCardViewModel(
            summary.Context.OfferingId,
            summary.Name,
            summary.Fulfillment.DisplayName,
            summary.Fulfillment.IsVariableProviderNetwork,
            status,
            summary.Counts.ActiveVariants,
            summary.Counts.ActiveDesignAreas,
            summary.Counts.ActiveMockupTemplates)
        {
            ReadyMockupTemplateCount = readiness?.ReadyMockupTemplateCount ?? 0,
            ReadinessSummary = readiness is null
                ? "Catalog readiness is unavailable."
                : ReadinessText(readiness),
            ReadinessGuidance = readiness?.Issues.Select(OfferingReadinessMessageTranslator.Translate).ToArray() ?? []
        };
        return card;
    }

    private static string ReadinessText(OfferingReadinessSummary readiness) => readiness.Status switch
    {
        OfferingReadinessStatus.ReadyForMockupGeneration => $"{readiness.ReadyMockupTemplateCount} Mockup Template{(readiness.ReadyMockupTemplateCount == 1 ? string.Empty : "s")} ready for mockup generation",
        OfferingReadinessStatus.NeedsAttention => "Mockup Templates need attention before they can be used",
        _ => "Catalog setup is incomplete"
    };
}
