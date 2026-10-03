using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

internal static class OfferingReadinessBuilder
{
    public static OfferingReadinessSummary Build(WorkspaceSnapshot snapshot, BlueprintOffering offering)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(offering);

        var variants = snapshot.OfferingVariants
            .Where(value => value.OfferingId == offering.Id && !value.IsArchived)
            .ToArray();
        var designAreas = snapshot.OfferingPlaceholders
            .Where(value => value.OfferingId == offering.Id && !value.IsArchived)
            .ToArray();
        var templates = snapshot.MockupTemplates
            .Where(value => value.BlueprintOfferingId == offering.Id && !value.IsArchived)
            .ToArray();

        var issues = new List<OfferingReadinessIssue>();
        if (variants.Length == 0)
            issues.Add(new(OfferingReadinessIssueKind.MissingVariants));
        if (designAreas.Length == 0)
            issues.Add(new(OfferingReadinessIssueKind.MissingDesignAreas));
        if (templates.Length == 0)
            issues.Add(new(OfferingReadinessIssueKind.MissingMockupTemplates));

        var readyTemplateCount = 0;
        foreach (var template in templates.OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase))
        {
            var readiness = MockupTemplateReadinessEvaluator.Evaluate(snapshot, template);
            if (readiness.IsReadyForUse)
            {
                readyTemplateCount++;
                continue;
            }

            issues.Add(new(
                OfferingReadinessIssueKind.IncompleteMockupTemplate,
                template.Name,
                readiness.Blockers));
        }

        var status = variants.Length == 0 || designAreas.Length == 0 || templates.Length == 0
            ? OfferingReadinessStatus.Incomplete
            : readyTemplateCount > 0
                ? OfferingReadinessStatus.ReadyForMockupGeneration
                : OfferingReadinessStatus.NeedsAttention;

        return new(variants.Length, designAreas.Length, templates.Length, readyTemplateCount, status, issues);
    }
}
