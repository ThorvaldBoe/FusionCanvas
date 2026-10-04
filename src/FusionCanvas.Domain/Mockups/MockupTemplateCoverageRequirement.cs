namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateCoverageRequirement(
    string Key,
    MockupTemplateCoverageStatus Status,
    IReadOnlyList<Guid> VariantIds,
    IReadOnlyList<string> VariantNames,
    IReadOnlyList<MockupTemplateCoverageOptionValue> Applicability,
    IReadOnlyList<Guid> MatchedSourceImageIds,
    string Explanation)
{
    public bool IsActionable => Status is MockupTemplateCoverageStatus.Missing or MockupTemplateCoverageStatus.Incomplete;
    public string VariantSummary => string.Join(", ", VariantNames);
}
