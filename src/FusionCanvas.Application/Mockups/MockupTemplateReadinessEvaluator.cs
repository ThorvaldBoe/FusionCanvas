using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

internal static class MockupTemplateReadinessEvaluator
{
    public static MockupTemplateReadinessResult Evaluate(WorkspaceSnapshot snapshot, MockupTemplate template)
    {
        var revision = snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id
            && value.RevisionNumber == template.CurrentRevision)
            ?? new MockupTemplateRevision(template.Id, template.Id, template.CurrentRevision, template.TargetPlaceholderId, template.CreatedAt);
        var colors = snapshot.MockupTemplateColorVariants.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived)
            .Select(value => value.ColorOptionValueId).ToArray();
        return MockupTemplateReadinessPolicy.Evaluate(new(template, revision, colors,
            snapshot.OfferingOptions, snapshot.OfferingOptionValues, snapshot.OfferingVariants, snapshot.OfferingPlaceholders,
            SourceImages: snapshot.MockupTemplateSourceImages, SourceImageOptionValues: snapshot.MockupTemplateSourceImageOptionValues));
    }
}
