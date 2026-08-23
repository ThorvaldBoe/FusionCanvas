namespace FusionCanvas.Application.Catalog;

public sealed record BulkVariantPreview(BulkVariantRequest Request, bool CanConfirm, string? Message, IReadOnlyList<BulkVariantCandidate> Candidates);
