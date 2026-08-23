using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.Catalog;

public sealed record BulkVariantResult(bool Succeeded, string? Error, IReadOnlyList<OfferingVariant> CreatedVariants, BulkVariantPreview Preview);
