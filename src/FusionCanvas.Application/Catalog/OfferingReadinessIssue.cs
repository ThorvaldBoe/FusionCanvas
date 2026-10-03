using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public sealed record OfferingReadinessIssue(
    OfferingReadinessIssueKind Kind,
    string? TemplateName = null,
    IReadOnlyList<MockupTemplateReadinessBlocker>? TemplateBlockers = null);
