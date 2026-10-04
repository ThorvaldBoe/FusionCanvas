namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateCoveragePlan(
    Guid TemplateId,
    Guid? TargetDesignAreaId,
    MockupTemplateCoverageGroupingStrategy GroupingStrategy,
    string ContextFingerprint,
    IReadOnlyList<MockupTemplateCoverageRequirement> Requirements,
    IReadOnlyList<Guid> IncompleteSourceImageIds,
    bool HasTargetDesignArea,
    string? Error = null)
{
    public int VariantCount => Requirements.SelectMany(value => value.VariantIds).Distinct().Count();
    public int ResolvedCount => Requirements.Where(value => value.Status == MockupTemplateCoverageStatus.Resolved).Sum(value => value.VariantIds.Count);
    public int MissingCount => Requirements.Where(value => value.Status == MockupTemplateCoverageStatus.Missing).Sum(value => value.VariantIds.Count);
    public int AmbiguousCount => Requirements.Where(value => value.Status == MockupTemplateCoverageStatus.Ambiguous).Sum(value => value.VariantIds.Count);
    public int IncompleteCount => Requirements.Where(value => value.Status == MockupTemplateCoverageStatus.Incomplete).Sum(value => value.VariantIds.Count);
    public bool IsComplete => HasTargetDesignArea && VariantCount > 0 && ResolvedCount == VariantCount && AmbiguousCount == 0 && MissingCount == 0 && IncompleteCount == 0;
    public bool IsStaleAgainst(MockupTemplateCoverageContext context) => context is null || context.Fingerprint != ContextFingerprint;
    public string Summary => HasTargetDesignArea
        ? $"{ResolvedCount}/{VariantCount} Variants covered; {MissingCount} missing, {AmbiguousCount} ambiguous, {IncompleteCount} incomplete."
        : "Choose a target Design Area before planning coverage.";
}
