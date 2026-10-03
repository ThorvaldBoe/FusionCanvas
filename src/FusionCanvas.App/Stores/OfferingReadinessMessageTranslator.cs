using FusionCanvas.App.Mockups;
using FusionCanvas.Application.Catalog;

namespace FusionCanvas.App.Stores;

internal static class OfferingReadinessMessageTranslator
{
    public static string Translate(OfferingReadinessIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return issue.Kind switch
        {
            OfferingReadinessIssueKind.MissingVariants => "Add at least one active Variant.",
            OfferingReadinessIssueKind.MissingDesignAreas => "Add at least one active Design Area.",
            OfferingReadinessIssueKind.MissingMockupTemplates => "Add a Mockup Template for this Offering.",
            OfferingReadinessIssueKind.IncompleteMockupTemplate => TranslateTemplate(issue),
            _ => "Review the Offering setup."
        };
    }

    private static string TranslateTemplate(OfferingReadinessIssue issue)
    {
        var name = string.IsNullOrWhiteSpace(issue.TemplateName) ? "Unnamed Mockup Template" : issue.TemplateName;
        var blockers = issue.TemplateBlockers ?? [];
        var guidance = blockers.Count == 0
            ? "Complete its readiness requirements."
            : string.Join(" ", blockers.Select(MockupTemplateReadinessMessageTranslator.Translate));
        return $"{name}: {guidance}";
    }
}
