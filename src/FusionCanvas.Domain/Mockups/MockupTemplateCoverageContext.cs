namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateCoverageContext(
    Guid TemplateId,
    Guid? TargetDesignAreaId,
    IReadOnlyList<Guid> VariantIds,
    IReadOnlyList<Guid> OptionValueIds)
{
    public Guid TemplateId { get; } = TemplateId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(TemplateId)) : TemplateId;
    public IReadOnlyList<Guid> VariantIds { get; } = VariantIds ?? throw new ArgumentNullException(nameof(VariantIds));
    public IReadOnlyList<Guid> OptionValueIds { get; } = OptionValueIds ?? throw new ArgumentNullException(nameof(OptionValueIds));

    public string Fingerprint => string.Join("|", new[]
    {
        TemplateId.ToString("N"),
        TargetDesignAreaId?.ToString("N") ?? "none",
        string.Join(",", VariantIds.OrderBy(value => value).Select(value => value.ToString("N"))),
        string.Join(",", OptionValueIds.OrderBy(value => value).Select(value => value.ToString("N")))
    });
}
